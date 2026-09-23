using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using BedrockLauncher.UpdateProcessor.Handlers;

byte[] data = new byte[ParallelDownloader.MinimumSize + 13];
new Random(42).NextBytes(data);
foreach (string mode in new[] { "valid", "ignore", "ignore-workers", "wrong-range", "changed", "truncated", "oversized", "no-tag", "cancel" })
{
    string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bedli-test");
    using var handler = new FakeServer(data, mode);
    using var client = new HttpClient(handler);
    using var cancel = new CancellationTokenSource();
    if (mode == "cancel") cancel.CancelAfter(30);
    long last = 0;
    try
    {
        bool success = await ParallelDownloader.TryDownloadAsync(client, "https://test.invalid/package", path,
            (current, total) =>
            {
                if (current < last || current > total || total != data.Length) throw new Exception("Bad progress.");
                last = current;
            }, cancel.Token);
        if (success != (mode == "valid")) throw new Exception($"Unexpected result for {mode}.");
        if (success && (!File.ReadAllBytes(path).SequenceEqual(data) || last != data.Length || handler.MaxActive < 2 || handler.MaxActive > 4))
            throw new Exception("Parallel data or concurrency mismatch.");
        if (!success && File.Exists(path)) throw new Exception("Failed download was published.");
        if (mode == "cancel") throw new Exception("Cancellation was swallowed.");
    }
    catch (OperationCanceledException) when (mode == "cancel") { }
    finally
    {
        if (File.Exists(path + ".parallel")) throw new Exception("Temporary file was left behind.");
        File.Delete(path);
    }
}
ProgressTests.Run();
Console.WriteLine("Passed 9 download tests and redirected progress rendering.");

sealed class FakeServer(byte[] data, string mode) : HttpMessageHandler
{
    int active;
    public int MaxActive;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        var range = request.Headers.Range.Ranges.Single();
        long start = range.From.Value, end = range.To.Value;
        bool probe = start == 0 && end == 0;
        if (!probe)
        {
            int count = Interlocked.Increment(ref active);
            MaxActive = Math.Max(MaxActive, count);
            try { await Task.Delay(100, token); }
            finally { Interlocked.Decrement(ref active); }
            if (request.Headers.IfRange?.EntityTag?.Tag != "\"stable\"") throw new Exception("Missing If-Range.");
        }
        var response = new HttpResponseMessage(mode == "ignore" || (mode == "ignore-workers" && !probe) ? HttpStatusCode.OK : HttpStatusCode.PartialContent);
        int length = (int)(end - start + 1);
        if (mode == "truncated" && !probe) length--;
        response.Content = new ByteArrayContent(data, (int)start, length);
        if (mode == "oversized" && !probe) response.Content = new ByteArrayContent(new byte[length + 1]);
        response.Content.Headers.ContentRange = new ContentRangeHeaderValue(mode == "wrong-range" && !probe ? start + 1 : start, end, data.Length);
        if (mode != "no-tag") response.Headers.ETag = new EntityTagHeaderValue(mode == "changed" && !probe ? "\"changed\"" : "\"stable\"");
        return response;
    }
}
