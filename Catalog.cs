using System.Text.Json;
namespace Bedli;
public sealed record BedrockVersion(string Version,string PackageVersion,string Arch,string Channel,List<string> Urls);
public static class Catalog {
 const string Source="https://raw.githubusercontent.com/MinecraftBedrockArchiver/GdkLinks/refs/heads/master/urls.json";
 static readonly HttpClient Http=new(){Timeout=TimeSpan.FromSeconds(30)};
 public static async Task<List<BedrockVersion>> GetAsync(){
   using var s=await Http.GetStreamAsync(Source); using var doc=await JsonDocument.ParseAsync(s); var list=new List<BedrockVersion>();
   foreach(var channel in doc.RootElement.EnumerateObject()) Walk(channel.Value,null,channel.Name,list);
   return list.GroupBy(x=>(x.Version,x.Arch,x.Channel)).Select(g=>g.First()).OrderByDescending(x=>Parse(x.Version)).ToList();
 }
 static void Walk(JsonElement e,string? key,string channel,List<BedrockVersion> list){
   if(e.ValueKind==JsonValueKind.Object){foreach(var p in e.EnumerateObject()) Walk(p.Value,p.Name,channel,list); return;}
   if(e.ValueKind==JsonValueKind.Array && key!=null){
     var urls=e.EnumerateArray().Where(x=>x.ValueKind==JsonValueKind.String).Select(x=>x.GetString()!).Where(x=>x.StartsWith("http")&&x.Contains("Minecraft",StringComparison.OrdinalIgnoreCase)).ToList();
     if(urls.Count>0){foreach(var g in urls.GroupBy(Arch))list.Add(new(Display(key),key,g.Key,channel,g.ToList())); return;}
     foreach(var x in e.EnumerateArray()) Walk(x,key,channel,list);
   }
 }
 static string Arch(string u){var l=u.ToLowerInvariant();if(l.Contains("_arm64_"))return "arm64";if(l.Contains("_x86_"))return "x86";return "x64";}
 static string Display(string v){var p=v.Split('.');if(p.Length>=4&&p[0]=="1"&&int.TryParse(p[1],out var m)&&m>=26&&int.TryParse(p[2],out var b)){if(b>=100)return $"{m}.{b/100}.{b%100}";return $"{m}.{b}.{p[3]}";}return v;}
 static Version Parse(string v)=>Version.TryParse(v,out var x)?x:new Version(0,0);
 public static async Task<BedrockVersion> FindAsync(string version,string arch){var all=await GetAsync();return all.FirstOrDefault(x=>x.Version.Equals(version,StringComparison.OrdinalIgnoreCase)&&x.Arch==arch)??throw new BedliException($"Version not found: {version} ({arch})");}
}