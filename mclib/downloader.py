import os, requests, concurrent.futures
from pathlib import Path
from .catalog import find, UA

ROOT=Path.home()/".mclib"
DOWNLOADS=ROOT/"downloads"
WORKERS=16
CHUNK=1024*1024

class DownloadError(RuntimeError): pass

def _parallel_download(url,dest,version):
    head=requests.head(url,headers={"User-Agent":UA},allow_redirects=True,timeout=30)
    head.raise_for_status()
    total=int(head.headers.get("Content-Length") or 0)
    ranges="bytes" in head.headers.get("Accept-Ranges","").lower()
    if total<=0 or not ranges:
        with requests.get(url,headers={"User-Agent":UA},stream=True,timeout=120) as r:
            r.raise_for_status()
            with dest.open("wb") as out:
                done=0
                for chunk in r.iter_content(CHUNK):
                    if chunk:
                        out.write(chunk); done+=len(chunk)
                        if total: print(f"Downloading {version}: {done*100//total}% ({done//1048576}/{total//1048576} MiB)",end="\r",flush=True)
        print(); return

    partdir=dest.parent/(dest.name+".parts")
    partdir.mkdir(parents=True,exist_ok=True)
    piece=(total+WORKERS-1)//WORKERS

    def fetch(i):
        start=i*piece
        end=min(total-1,start+piece-1)
        if start>end:return 0
        p=partdir/f"{i:02d}.part"
        existing=p.stat().st_size if p.exists() else 0
        expected=end-start+1
        if existing==expected:return existing
        if existing>expected:
            p.unlink(); existing=0
        h={"User-Agent":UA,"Range":f"bytes={start+existing}-{end}"}
        with requests.get(url,headers=h,stream=True,timeout=120) as r:
            if r.status_code!=206:
                raise DownloadError(f"CDN did not honor HTTP range request for worker {i} (HTTP {r.status_code})")
            with p.open("ab") as out:
                for chunk in r.iter_content(CHUNK):
                    if chunk: out.write(chunk)
        if p.stat().st_size!=expected:
            raise DownloadError(f"Worker {i} downloaded an incomplete range")
        return expected

    print(f"Downloading {version}: {total//1048576} MiB with {WORKERS} workers")
    with concurrent.futures.ThreadPoolExecutor(max_workers=WORKERS) as ex:
        futures=[ex.submit(fetch,i) for i in range(WORKERS)]
        completed=0
        for fut in concurrent.futures.as_completed(futures):
            completed+=fut.result()
            print(f"Downloading {version}: {completed*100//total}% ({completed//1048576}/{total//1048576} MiB)",end="\r",flush=True)
    print()
    tmp=dest.with_suffix(dest.suffix+".download")
    with tmp.open("wb") as out:
        for i in range(WORKERS):
            p=partdir/f"{i:02d}.part"
            with p.open("rb") as inp:
                while True:
                    b=inp.read(8*CHUNK)
                    if not b:break
                    out.write(b)
    if tmp.stat().st_size!=total:
        raise DownloadError("Assembled package size does not match CDN Content-Length")
    os.replace(tmp,dest)
    import shutil; shutil.rmtree(partdir,ignore_errors=True)

def download_version(version,arch="x64",channel=None):
    entry=find(version,arch,channel)
    if not entry: raise DownloadError(f"Bedrock version not found: {version} ({arch})")
    if not entry.get("urls"):
        raise DownloadError(f"{version} is in the catalog but requires the Microsoft Store entitlement/update-link flow.")
    DOWNLOADS.mkdir(parents=True,exist_ok=True)
    last=None
    for url in entry["urls"]:
        try:
            filename=url.split("?")[0].rsplit("/",1)[-1] or f"minecraft-{version}.package"
            dest=DOWNLOADS/filename
            if not dest.exists(): _parallel_download(url,dest,version)
            return dest,entry
        except Exception as e:
            last=e
    raise DownloadError(f"All Bedrock package mirrors failed: {last}")
