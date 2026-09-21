from PyInstaller.__main__ import run
from pathlib import Path
import shutil, os
root=Path(__file__).resolve().parents[1]
dist=root/"dist"; build=root/"build"; spec=root/"mclib.spec"
for p in (dist,build): shutil.rmtree(p,ignore_errors=True)
if spec.exists(): spec.unlink()
entry=root/"mclib_pyinstaller_entry.py"
entry.write_text("from mclib.cli import main\nif __name__ == '__main__':\n    main()\n",encoding="utf-8")
try:
    run([str(entry),"--name=bedli","--onefile","--clean","--noconfirm",f"--distpath={dist}",f"--workpath={build}",f"--specpath={root}","--collect-submodules=mclib"])
finally:
    entry.unlink(missing_ok=True)
exe=dist/"bedli.exe"
if not exe.exists(): raise SystemExit("bedli.exe was not produced")
print(exe)
