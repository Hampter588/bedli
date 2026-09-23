using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace BedrockLauncher.UpdateProcessor.Handlers
{
    internal static class ParallelDownloader
    {
        // Small files and resumable single-stream downloads keep the existing path.
        internal const long MinimumSize = 16 * 1024 * 1024;
        internal static async Task<bool> TryDownloadAsync(HttpClient client, string url, string destination,
            Action<long, long> progress, CancellationToken cancellationToken)
        {
            string temporary = destination + ".parallel";
            try
            {
                using var probe = Request(url, 0, 0);
                using var response = await client.SendAsync(probe, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                var range = response.Content.Headers.ContentRange;
                var tag = response.Headers.ETag;
                long size = range?.Length ?? 0;
                if (response.StatusCode != HttpStatusCode.PartialContent || range?.Unit != "bytes" ||
                    range.From != 0 || range.To != 0 || size < MinimumSize || tag == null || tag.IsWeak ||
                    response.Content.Headers.ContentEncoding.Count != 0)
                    return false;

                response.Dispose(); // Release the probe connection before starting workers.

                using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    file.SetLength(size);
                using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var gate = new object();
                long completed = 0;
                var clock = Stopwatch.StartNew();
                progress?.Invoke(0, size);
                var tasks = Enumerable.Range(0, 4).Select(async index =>
                {
                    try
                    {
                        long start = size * index / 4;
                        long end = size * (index + 1) / 4 - 1;
                        using var request = Request(url, start, end);
                        request.Headers.IfRange = new RangeConditionHeaderValue(tag);
                        using var part = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, stop.Token);
                        var contentRange = part.Content.Headers.ContentRange;
                        if (part.StatusCode != HttpStatusCode.PartialContent || contentRange?.Unit != "bytes" ||
                            contentRange.From != start || contentRange.To != end || contentRange.Length != size ||
                            part.Content.Headers.ContentEncoding.Count != 0 ||
                            (part.Headers.ETag != null && !part.Headers.ETag.Equals(tag)))
                            throw new InvalidDataException("Server did not return the requested unchanged byte range.");

                        using var input = await part.Content.ReadAsStreamAsync(stop.Token);
                        using var output = new FileStream(temporary, FileMode.Open, FileAccess.Write,
                            FileShare.ReadWrite, 128 * 1024, FileOptions.Asynchronous);
                        output.Position = start;
                        byte[] buffer = new byte[128 * 1024];
                        long remaining = end - start + 1;
                        while (remaining > 0)
                        {
                            int read = await input.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), stop.Token);
                            if (read == 0) throw new InvalidDataException("Truncated download range.");
                            await output.WriteAsync(buffer.AsMemory(0, read), stop.Token);
                            remaining -= read;
                            lock (gate)
                            {
                                completed += read;
                                if (clock.ElapsedMilliseconds >= 200)
                                {
                                    progress?.Invoke(completed, size);
                                    clock.Restart();
                                }
                            }
                        }
                        if (await input.ReadAsync(buffer.AsMemory(0, 1), stop.Token) != 0)
                            throw new InvalidDataException("Oversized download range.");
                    }
                    catch { stop.Cancel(); throw; }
                }).ToArray();
                try { await Task.WhenAll(tasks); }
                catch
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // A range worker can cancel its siblings; retry safely as one stream.
                    return false;
                }
                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporary, destination, true);
                progress?.Invoke(size, size);
                return true;
            }
            catch (HttpRequestException) { return false; }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        private static HttpRequestMessage Request(string url, long from, long to)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("bedli");
            request.Headers.AcceptEncoding.ParseAdd("identity");
            request.Headers.Range = new RangeHeaderValue(from, to);
            return request;
        }
    }
}
