import os, requests
from pathlib import Path
from .catalog import find, UA

ROOT=Path.home()/".mclib"
DOWNLOADS=ROOT/"downloads"

class DownloadError(RuntimeError): pass

def download_version(version,arch="x64",channel=None):
    entry=find(version,arch,channel)
    if not entry: raise DownloadError(f"Bedrock version not found: {version} ({arch})")
    if not entry.get("urls"):
        raise DownloadError(
            f"{version} is in BedrockLauncher's community catalog, but requires the Microsoft Store "
            "entitlement/update-link flow. Direct package URL is not published for this entry yet."
        )
    DOWNLOADS.mkdir(parents=True,exist_ok=True)
    last=None
    for url in entry["urls"]:
        try:
            filename=url.split("?")[0].rsplit("/",1)[-1] or f"minecraft-{version}.package"
            dest=DOWNLOADS/filename
            partial=dest.with_suffix(dest.suffix+".download")
            existing=partial.stat().st_size if partial.exists() else 0
            headers={"User-Agent":UA}
            if existing: headers["Range"]=f"bytes={existing}-"
            with requests.get(url,headers=headers,stream=True,timeout=120) as r:
                if existing and r.status_code==200:
                    partial.unlink(missing_ok=True); existing=0
                r.raise_for_status()
                total=int(r.headers.get("Content-Length") or 0)+existing
                mode="ab" if existing else "wb"; done=existing
                with partial.open(mode) as f:
                    for chunk in r.iter_content(1024*1024):
                        if not chunk: continue
                        f.write(chunk); done+=len(chunk)
                        if total:
                            print(f"Downloading {version}: {done*100//total}% ({done//1048576}/{total//1048576} MiB)",end="\r",flush=True)
            print()
            os.replace(partial,dest)
            return dest,entry
        except Exception as e:
            last=e
    raise DownloadError(f"All BedrockLauncher package mirrors failed: {last}")
