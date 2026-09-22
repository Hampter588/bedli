using System.Diagnostics;
using Windows.Management.Deployment;
namespace Bedli;
public static class PackageService {
 const string ReleaseFamily="Microsoft.MinecraftUWP_8wekyb3d8bbwe";
 const string PreviewFamily="Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe";
 static string Family(string channel)=>channel=="preview"?PreviewFamily:ReleaseFamily;
 public static async Task<string> InstallAsync(BedrockVersion v,string package){
  Paths.Ensure();var family=Family(v.Channel);var pm=new PackageManager();
  Console.WriteLine("Staging MSIXVC through Windows Gaming Services...");
  try{await pm.StagePackageAsync(new Uri(Path.GetFullPath(package)),null);}
  catch(Exception e){throw new BedliException($"Windows could not stage the package (0x{e.HResult:X8}): {e.Message}\nInstall Minecraft for Windows from Microsoft Store first so Gaming Services has your license/decryption keys.");}
  var packages=pm.FindPackagesForUser("",family).ToList();if(packages.Count==0)throw new BedliException("Package staged, but Minecraft package registration was not found.");
  var pkg=packages.OrderByDescending(p=>p.Id.Version.Major).ThenByDescending(p=>p.Id.Version.Minor).ThenByDescending(p=>p.Id.Version.Build).ThenByDescending(p=>p.Id.Version.Revision).First();
  var staged=pkg.InstalledLocation.Path;var srcExe=Path.Combine(staged,"Minecraft.Windows.exe");if(!File.Exists(srcExe))throw new BedliException("Staged Minecraft.Windows.exe was not found at "+staged);
  var dest=Path.Combine(Paths.Versions,v.Version);if(Directory.Exists(dest))Directory.Delete(dest,true);Directory.CreateDirectory(dest);
  var tmp=Path.Combine(Path.GetTempPath(),$"bedli-{Guid.NewGuid():N}.exe");
  await DecryptExe(family,srcExe,tmp);
  Console.WriteLine("Copying staged game files...");
  CopyTree(staged,dest,srcExe);File.Copy(tmp,Path.Combine(dest,"Minecraft.Windows.exe"),true);File.Delete(tmp);
  if(!File.Exists(Path.Combine(dest,"MicrosoftGame.Config")))Console.WriteLine("WARNING: MicrosoftGame.Config was not found in extracted version.");
  Console.WriteLine("Installed "+v.Version+" -> "+dest);return dest;
 }
 static async Task DecryptExe(string family,string src,string dst){
  var helper=Path.Combine(AppContext.BaseDirectory,"GDKDecryptHelper.exe");if(!File.Exists(helper))throw new BedliException("GDKDecryptHelper.exe is missing beside bedli.exe.");
  var log=Path.GetTempFileName();var done=dst+".done";var arg=$"\"{src}\" \"{dst}\" \"{log}\" \"{done}\"";
  string Escaped(string x)=>x.Replace("'","''");
  var command=$"Invoke-CommandInDesktopPackage -PackageFamilyName '{Escaped(family)}' -App Game -Command '{Escaped(helper)}' -Args '{Escaped(arg)}'";
  var psi=new ProcessStartInfo("powershell.exe"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true};
  psi.ArgumentList.Add("-NoProfile");psi.ArgumentList.Add("-NonInteractive");psi.ArgumentList.Add("-ExecutionPolicy");psi.ArgumentList.Add("Bypass");psi.ArgumentList.Add("-Command");psi.ArgumentList.Add(command);
  using var p=Process.Start(psi)??throw new BedliException("Could not start PowerShell package-context helper.");await p.WaitForExitAsync();
  for(int i=0;i<300&&!File.Exists(done);i++)await Task.Delay(100);
  if(!File.Exists(dst)){var detail=File.Exists(log)?File.ReadAllText(log):await p.StandardError.ReadToEndAsync();throw new BedliException("Licensed executable extraction failed. "+detail);}
  File.Delete(done);File.Delete(log);
 }
 static void CopyTree(string src,string dst,string skip){
  Directory.CreateDirectory(dst);foreach(var f in Directory.EnumerateFiles(src)){if(Path.GetFullPath(f).Equals(Path.GetFullPath(skip),StringComparison.OrdinalIgnoreCase))continue;try{File.Copy(f,Path.Combine(dst,Path.GetFileName(f)),true);}catch(UnauthorizedAccessException){}}
  foreach(var d in Directory.EnumerateDirectories(src)){try{CopyTree(d,Path.Combine(dst,Path.GetFileName(d)),skip);}catch(UnauthorizedAccessException){}}
 }
 public static IEnumerable<string> Installed(){Paths.Ensure();return Directory.EnumerateDirectories(Paths.Versions).Where(d=>File.Exists(Path.Combine(d,"Minecraft.Windows.exe"))).Select(Path.GetFileName)!;}
 public static Process Launch(string version){var dir=Path.Combine(Paths.Versions,version);var exe=Path.Combine(dir,"Minecraft.Windows.exe");if(!File.Exists(exe))throw new BedliException("Version is not installed: "+version);return Process.Start(new ProcessStartInfo(exe){WorkingDirectory=dir,UseShellExecute=false})??throw new BedliException("Minecraft process did not start.");}
 public static void Remove(string version){var dir=Path.Combine(Paths.Versions,version);if(!Directory.Exists(dir))throw new BedliException("Version is not installed: "+version);Directory.Delete(dir,true);}
}