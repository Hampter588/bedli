using System.Net;
using System.Net.Http.Headers;
namespace Bedli;
public static class Downloader {
 const int Workers=16; static readonly HttpClient Http=new(){Timeout=TimeSpan.FromMinutes(20)};
 public static async Task<string> DownloadAsync(BedrockVersion v){
  Paths.Ensure(); Exception? last=null;
  foreach(var url in v.Urls) try{return await DownloadUrl(url,v.Version);}catch(Exception e){last=e;}
  throw new BedliException("All package mirrors failed: "+last?.Message);
 }
 static async Task<string> DownloadUrl(string url,string version){
  var name=Path.GetFileName(new Uri(url).LocalPath); if(string.IsNullOrWhiteSpace(name))name=$"minecraft-{version}.msixvc";
  var dest=Path.Combine(Paths.Downloads,name); if(File.Exists(dest))return dest;
  using var head=new HttpRequestMessage(HttpMethod.Head,url); using var hr=await Http.SendAsync(head); hr.EnsureSuccessStatusCode();
  var total=hr.Content.Headers.ContentLength??0; var ranges=hr.Headers.AcceptRanges.Contains("bytes");
  if(total<=0||!ranges){using var r=await Http.GetAsync(url,HttpCompletionOption.ResponseHeadersRead);r.EnsureSuccessStatusCode();await using var src=await r.Content.ReadAsStreamAsync();await using var dst=File.Create(dest);await src.CopyToAsync(dst);return dest;}
  var partDir=dest+".parts";Directory.CreateDirectory(partDir);long piece=(total+Workers-1)/Workers;long done=0;
  await Parallel.ForEachAsync(Enumerable.Range(0,Workers),new ParallelOptions{MaxDegreeOfParallelism=Workers},async(i,ct)=>{
    long start=i*piece,end=Math.Min(total-1,start+piece-1);if(start>end)return;var p=Path.Combine(partDir,$"{i:D2}.part");long have=File.Exists(p)?new FileInfo(p).Length:0;long expected=end-start+1;if(have==expected){Interlocked.Add(ref done,have);return;}if(have>expected){File.Delete(p);have=0;}
    using var req=new HttpRequestMessage(HttpMethod.Get,url);req.Headers.Range=new RangeHeaderValue(start+have,end);using var res=await Http.SendAsync(req,HttpCompletionOption.ResponseHeadersRead,ct);if(res.StatusCode!=HttpStatusCode.PartialContent)throw new BedliException($"CDN refused range worker {i}: {(int)res.StatusCode}");
    await using var src=await res.Content.ReadAsStreamAsync(ct);await using var fs=new FileStream(p,FileMode.Append,FileAccess.Write,FileShare.None,1<<20,true);var buf=new byte[1<<20];int n;while((n=await src.ReadAsync(buf,ct))>0){await fs.WriteAsync(buf.AsMemory(0,n),ct);Interlocked.Add(ref done,n);Console.Write($"\rDownloading {version}: {Math.Min(100,done*100/total)}%  ");}
  });
  Console.WriteLine();await using(var output=File.Create(dest)){for(int i=0;i<Workers;i++){var p=Path.Combine(partDir,$"{i:D2}.part");if(!File.Exists(p))continue;await using var input=File.OpenRead(p);await input.CopyToAsync(output);}}
  if(new FileInfo(dest).Length!=total){File.Delete(dest);throw new BedliException("Assembled package size mismatch.");}Directory.Delete(partDir,true);return dest;
 }
}