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
