import json, os, subprocess
from pathlib import Path

MINECRAFT_NAMES = {
    "Microsoft.MinecraftUWP",
    "Microsoft.MinecraftWindowsBeta",
}

class BedrockError(RuntimeError):
    pass

def _powershell(script):
    p=subprocess.run(
        ["powershell","-NoProfile","-ExecutionPolicy","Bypass","-Command",script],
        capture_output=True,text=True,encoding="utf-8",errors="replace"
    )
    if p.returncode:
        raise BedrockError((p.stderr or p.stdout).strip() or "PowerShell command failed")
    return p.stdout

def _json_ps(script):
    out=_powershell(script + " | ConvertTo-Json -Depth 6 -Compress")
    if not out.strip():
        return []
    data=json.loads(out)
    return data if isinstance(data,list) else [data]

def packages():
    rows=_json_ps(r'''
Get-AppxPackage -AllUsers | Where-Object {
  $_.Name -eq 'Microsoft.MinecraftUWP' -or
  $_.Name -eq 'Microsoft.MinecraftWindowsBeta'
} | Select-Object Name, PackageFullName, PackageFamilyName, Version, InstallLocation,
    @{N='Publisher';E={$_.Publisher}},
    @{N='Architecture';E={$_.Architecture.ToString()}},
    @{N='IsFramework';E={$_.IsFramework}}
''')
    return rows

def app_ids(package_family_name):
    # Query registered shell apps and select entries belonging to the package family.
    rows=_json_ps(rf'''
Get-StartApps | Where-Object {{ $_.AppID -like '{package_family_name}!*' }} |
  Select-Object Name, AppID
''')
    return rows

def launch(package=None):
    pkgs=packages()
    if not pkgs:
        raise BedrockError("Minecraft for Windows is not installed.")

    if package:
        matches=[p for p in pkgs if package.lower() in str(p.get("Version","")).lower()
                 or package.lower() in p.get("PackageFullName","").lower()
                 or package.lower() in p.get("Name","").lower()]
        if not matches:
            raise BedrockError(f"No installed Bedrock package matched: {package}")
        pkg=matches[0]
    else:
        # Prefer stable Minecraft over Preview/Beta, then highest version.
        stable=[p for p in pkgs if p.get("Name")=="Microsoft.MinecraftUWP"]
        choices=stable or pkgs
        def ver(p):
            try: return tuple(int(x) for x in str(p.get("Version","0")).split("."))
            except Exception: return (0,)
        pkg=max(choices,key=ver)

    ids=app_ids(pkg["PackageFamilyName"])
    if not ids:
        raise BedrockError("Could not determine Minecraft AppUserModelId.")
    appid=ids[0]["AppID"]
    subprocess.Popen(["explorer.exe",f"shell:AppsFolder\\{appid}"])
    return pkg,appid

def install(package_path):
    path=Path(package_path).expanduser().resolve()
    if not path.exists():
        raise BedrockError(f"Package not found: {path}")
    if path.suffix.lower() not in (".msix",".appx",".msixbundle",".appxbundle"):
        raise BedrockError("Expected .msix, .msixbundle, .appx, or .appxbundle package.")
    escaped=str(path).replace("'","''")
    _powershell(f"Add-AppxPackage -Path '{escaped}'")
    return path

def uninstall(package_full_name):
    if not package_full_name:
        raise BedrockError("Package full name is required.")
    escaped=package_full_name.replace("'","''")
    _powershell(f"Remove-AppxPackage -Package '{escaped}'")
