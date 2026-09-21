import json, platform, re
from pathlib import Path
import requests

COMMUNITY_URL="https://www.raythnetwork.co.uk/versions.php?type=json"
GDK_URL="https://cdn.jsdelivr.net/gh/MinecraftBedrockArchiver/GdkLinks@latest/urls.json"
UA="MCLI-Bedrock/0.2 (https://github.com/Hampter588/mcli-bedrock)"

def _get_json(url):
    r=requests.get(url,timeout=30,headers={"User-Agent":UA})
    r.raise_for_status()
    return r.json()

def _display_gdk(package_version):
    parts=[int(x) if x.isdigit() else 0 for x in str(package_version).split(".")]
    parts=(parts+[0,0,0,0])[:4]
    major,minor,build,revision=parts
    if major==1 and minor>=26:
        if build>=100:
            feature,patch=divmod(build,100)
            return f"{minor}.{feature}.{patch}" if patch else f"{minor}.{feature}"
        return f"{minor}.{build}.{revision}" if revision else f"{minor}.{build}"
    return ".".join(str(x) for x in parts)

def _arch_from_url(url):
    low=url.lower()
    for token,name in (("_arm64_","arm64"),("_arm_","arm"),("_x86_","x86"),("_x64_","x64")):
        if token in low:return name
    return "x64"

def community_versions():
    rows=[]
    for item in _get_json(COMMUNITY_URL):
        if not isinstance(item,list) or len(item)<4: continue
        version,identity,kind,arch=item[:4]
        rows.append({"version":str(version),"id":str(identity),"channel":"preview" if str(kind) in ("1","2") else "release","arch":str(arch),"source":"rayth","urls":[]})
    return rows

def gdk_versions():
    raw=_get_json(GDK_URL)
    release=raw.get("release",raw) if isinstance(raw,dict) else {}
    rows=[]
    for package_version,value in release.items():
        urls=value if isinstance(value,list) else str(value).split()
        urls=[u for u in urls if "microsoft.minecraftuwp" in u.lower() and any(x in u.lower() for x in (".appx",".msix",".msixvc"))]
        if not urls: continue
        byarch={}
        for u in urls: byarch.setdefault(_arch_from_url(u),[]).append(u)
        for arch,links in byarch.items():
            rows.append({"version":_display_gdk(package_version),"package_version":package_version,"id":f"gdk:{package_version}:{arch}","channel":"release","arch":arch,"source":"gdklinks","urls":links})
    return rows

def versions():
    rows=community_versions()+gdk_versions()
    seen=set(); out=[]
    def key(v): return (v["version"],v["channel"],v["arch"])
    # Direct GDK entries win over duplicate community metadata.
    for v in sorted(rows,key=lambda x:x["source"]!="gdklinks"):
        k=key(v)
        if k in seen: continue
        seen.add(k); out.append(v)
    def vk(x):
        nums=[int(n) for n in re.findall(r"\d+",x["version"])]
        return tuple((nums+[0,0,0,0])[:4])
    return sorted(out,key=vk,reverse=True)

def find(version,arch=None,channel=None):
    matches=[v for v in versions() if v["version"].lower()==version.lower()]
    if arch: matches=[v for v in matches if v["arch"].lower()==arch.lower()]
    if channel: matches=[v for v in matches if v["channel"]==channel]
    if not matches:return None
    direct=[v for v in matches if v["urls"]]
    return (direct or matches)[0]
