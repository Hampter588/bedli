namespace Bedli;
public static class Cli {
 public static async Task RunAsync(string[] a){
  if(a.Length==0||a[0] is "-h" or "--help" or "help"){Help();return;} Paths.Ensure();
  if(a[0]=="__copy-exe"){Require(a,4);File.Copy(a[1],a[2],true);File.WriteAllText(a[3],"done");return;}
  switch(a[0].ToLowerInvariant()){
   case "versions": await Versions(a);break;
   case "download": Require(a,2);{var v=await Catalog.FindAsync(a[1],Opt(a,"--arch")??"x64");var p=await Downloader.DownloadAsync(v);Console.WriteLine(p);}break;
   case "install": Require(a,2);{var v=await Catalog.FindAsync(a[1],Opt(a,"--arch")??"x64");var p=await Downloader.DownloadAsync(v);await PackageService.InstallAsync(v,p);}break;
   case "installed": foreach(var x in PackageService.Installed())Console.WriteLine(x);break;
   case "launch": Require(a,2);{var p=PackageService.Launch(a[1]);Console.WriteLine($"Minecraft {a[1]} started (PID {p.Id})");}break;
   case "remove": Require(a,2);PackageService.Remove(a[1]);Console.WriteLine("Removed "+a[1]);break;
   default: throw new BedliException("Unknown command: "+a[0]);
  }
 }
 static async Task Versions(string[] a){var arch=Opt(a,"--arch");var all=await Catalog.GetAsync();if(arch!=null)all=all.Where(x=>x.Arch==arch).ToList();Console.WriteLine($"{"VERSION",-16} {"ARCH",-7} {"CHANNEL",-8} PACKAGE");foreach(var v in all.Take(100))Console.WriteLine($"{v.Version,-16} {v.Arch,-7} {v.Channel,-8} {v.PackageVersion}");}
 static string? Opt(string[] a,string n){var i=Array.IndexOf(a,n);return i>=0&&i+1<a.Length?a[i+1]:null;}
 static void Require(string[] a,int n){if(a.Length<n)throw new BedliException("Missing argument. Run bedli --help.");}
 static void Help(){Console.WriteLine("""BEDLI — Minecraft Bedrock command-line launcher

bedli versions [--arch x64]
bedli download <version> [--arch x64]
bedli install <version> [--arch x64]
bedli installed
bedli launch <version>
bedli remove <version>

GDK installs require a legitimate Minecraft for Windows Microsoft Store license.
""");}
}