import os, shutil, subprocess, tempfile
from pathlib import Path

ROOT=Path.home()/".mclib"
VERSIONS=ROOT/"versions"
REQUIRED=("Minecraft.Windows.exe","MicrosoftGame.Config")

class GdkError(RuntimeError): pass

def helper_path():
    import sys
    base=Path(sys.executable).resolve().parent if getattr(sys,"frozen",False) else Path(__file__).resolve().parents[1]
    p=base/"GDKDecryptHelper.exe"
    if not p.exists():
        p=base/"native"/"GDKDecryptHelper"/"GDKDecryptHelper.exe"
    return p

def validate(directory):
    d=Path(directory)
    missing=[x for x in REQUIRED if not (d/x).exists()]
    if not (d/"data").is_dir(): missing.append("data/")
    if missing: raise GdkError("Incomplete GDK version: missing "+", ".join(missing))
    return d

def import_extracted(version, source):
    src=validate(source)
    dest=VERSIONS/version
    if dest.exists(): shutil.rmtree(dest)
    dest.parent.mkdir(parents=True,exist_ok=True)
    shutil.copytree(src,dest)
    return validate(dest)

def decrypt_executable(source_exe, destination_exe):
    helper=helper_path()
    if not helper.exists(): raise GdkError("GDKDecryptHelper.exe is missing from this MCLI Bedrock build.")
    source_exe=Path(source_exe).resolve(); destination_exe=Path(destination_exe).resolve()
    destination_exe.parent.mkdir(parents=True,exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="mclib-gdk-") as td:
        log=Path(td)/"decrypt.log"; done=Path(td)/"done"
        p=subprocess.run([str(helper),str(source_exe),str(destination_exe),str(log),str(done)])
        if p.returncode or not destination_exe.exists():
            detail=log.read_text(errors="replace") if log.exists() else ""
            raise GdkError("Windows could not decrypt/copy Minecraft.Windows.exe. A licensed Store installation is required. "+detail)
    return destination_exe

def local_versions():
    if not VERSIONS.exists(): return []
    return [p for p in VERSIONS.iterdir() if p.is_dir() and (p/"Minecraft.Windows.exe").exists()]

def launch_local(version):
    d=validate(VERSIONS/version)
    return subprocess.Popen([str(d/"Minecraft.Windows.exe")],cwd=d)


def _ps(script):
    p=subprocess.run(["powershell","-NoProfile","-ExecutionPolicy","Bypass","-Command",script],
                     capture_output=True,text=True,encoding="utf-8",errors="replace")
    if p.returncode:
        raise GdkError((p.stderr or p.stdout).strip() or "PowerShell failed")
    return p.stdout.strip()

def _family(channel):
    return "Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe" if channel=="preview" else "Microsoft.MinecraftUWP_8wekyb3d8bbwe"

def _staged_location(family):
    script=f"""$p=Get-AppxPackage -AllUsers | Where-Object {{$_.PackageFamilyName -eq '{family}'}} | Select-Object -First 1 -ExpandProperty InstallLocation; if($p){{$p}}"""
    out=_ps(script)
    if not out: raise GdkError("Windows staged the package but its install location could not be found.")
    return Path(out.splitlines()[-1].strip()).resolve()

def _run_decrypt_in_package(family, src, dst):
    helper=helper_path().resolve()
    if not helper.exists(): raise GdkError("GDKDecryptHelper.exe is missing.")
    with tempfile.TemporaryDirectory(prefix="mclib-gdk-") as td:
        td=Path(td); log=td/"decrypt.log"; done=td/"done"
        args=f'\"{src}\" \"{dst}\" \"{log}\" \"{done}\"'
        ps=f"""Invoke-CommandInDesktopPackage -PackageFamilyName '{family}' -App Game -Command '{helper}' -Args '{args}'"""
        _ps(ps)
        import time
        for _ in range(600):
            if done.exists(): break
            time.sleep(.1)
        if not Path(dst).exists():
            detail=log.read_text(errors="replace") if log.exists() else ""
            raise GdkError("Could not decrypt Minecraft.Windows.exe. Install Minecraft from Microsoft Store with the licensed Windows account first. "+detail)

def extract_msixvc(version, package_path, channel="release"):
    package=Path(package_path).resolve()
    if not package.exists(): raise GdkError(f"Package not found: {package}")
    family=_family(channel)
    dest=(VERSIONS/version).resolve()
    if dest.exists(): shutil.rmtree(dest)
    dest.parent.mkdir(parents=True,exist_ok=True)

    # Same entitlement-aware strategy used by the GPL reference launcher:
    # let Windows/Gaming Services stage the encrypted XVC, then copy the
    # licensed executable from inside package context.
    uri=package.as_uri()
    _ps(f"""$ErrorActionPreference='Stop'; $pm=[Windows.Management.Deployment.PackageManager,Windows.Management.Deployment,ContentType=WindowsRuntime]::new(); $op=$pm.StagePackageAsync([Uri]'{uri}', $null); while($op.Status -eq 0){{Start-Sleep -Milliseconds 200}}; if($op.Status -ne 1){{$err=$op.ErrorCode; $hex=('0x{0:X8}' -f ($err.Value__ -band 0xffffffff)); $msg=try {{$err.ToString()}} catch {{'Unknown'}}; throw ('StagePackageAsync failed: '+$hex+' '+$msg)}}""")
    staged=_staged_location(family)
    exe_src=staged/"Minecraft.Windows.exe"
    if not exe_src.exists(): raise GdkError(f"Staged Minecraft executable not found: {exe_src}")

    tmp=Path(tempfile.gettempdir())/f"mclib-minecraft-{os.getpid()}.exe"
    tmp.unlink(missing_ok=True)
    _run_decrypt_in_package(family,exe_src,tmp)

    # Copy staged payload while skipping the protected executable, then replace
    # it with the licensed copy produced inside the package context.
    def ignore(path,names):
        return {"Minecraft.Windows.exe"} if Path(path)==staged and "Minecraft.Windows.exe" in names else set()
    shutil.copytree(staged,dest,ignore=ignore)
    shutil.copy2(tmp,dest/"Minecraft.Windows.exe")
    tmp.unlink(missing_ok=True)
    return validate(dest)

def install_gdk_version(version, arch="x64", channel="release"):
    from .downloader import download_version
    package,entry=download_version(version,arch,channel)
    if package.suffix.lower()!=".msixvc":
        raise GdkError(f"{version} resolved to {package.name}, not an MSIXVC GDK package.")
    return extract_msixvc(version,package,channel),entry
