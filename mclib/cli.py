import argparse, json, platform, sys
from .windows import BedrockError, packages, launch, install, uninstall, install_version

def require_windows():
    if platform.system()!="Windows":
        raise SystemExit("MCLI Bedrock currently supports Windows only.")

def cmd_packages(args):
    require_windows()
    rows=packages()
    if not rows:
        print("No Minecraft for Windows packages found.")
        return
    print(f'{"VERSION":18} {"NAME":34} ARCH')
    print("-"*72)
    for p in rows:
        print(f'{str(p.get("Version","")):18} {p.get("Name",""):34} {p.get("Architecture","")}')

def cmd_status(args):
    require_windows()
    rows=packages()
    if not rows:
        print("Minecraft for Windows: not installed")
        return
    for p in rows:
        print(f'{p.get("Name")} {p.get("Version")} — {p.get("PackageFullName")}')

def cmd_launch(args):
    require_windows()
    pkg,appid=launch(args.version)
    print(f'Launching {pkg.get("Name")} {pkg.get("Version")}')
    print(f'AppID: {appid}')

def cmd_install(args):
    require_windows()
    candidate=args.package
    from pathlib import Path
    if Path(candidate).expanduser().exists():
        path=install(candidate)
        print("Installed package:",path)
    else:
        path,entry=install_version(candidate,args.arch,args.channel)
        print(f"Installed Bedrock {entry['version']} from {entry['source']}")
        print(path)

def cmd_uninstall(args):
    require_windows()
    uninstall(args.package_full_name)
    print("Removed:",args.package_full_name)

def cmd_versions(args):
    require_windows()
    from .catalog import versions
    rows=versions()
    if args.channel: rows=[x for x in rows if x["channel"]==args.channel]
    if args.arch: rows=[x for x in rows if x["arch"]==args.arch]
    rows=rows[:args.limit]
    print(f'{"VERSION":18} {"CHANNEL":10} {"ARCH":8} {"SOURCE":10} DOWNLOAD')
    print("-"*70)
    for x in rows:
        mode="direct" if x["urls"] else "store"
        print(f'{x["version"]:18} {x["channel"]:10} {x["arch"]:8} {x["source"]:10} {mode}')

def cmd_download(args):
    require_windows()
    from .downloader import download_version
    path,entry=download_version(args.version,args.arch,args.channel)
    print("Downloaded:",path)
    print("Source:",entry["source"])

def cmd_gdk(args):
    require_windows()
    from .gdk import import_extracted, local_versions, launch_local
    if args.gdk_action=="import":
        d=import_extracted(args.version,args.directory)
        print("Imported GDK version:",d)
    elif args.gdk_action=="list":
        for d in local_versions(): print(d.name)
    elif args.gdk_action=="launch":
        p=launch_local(args.version)
        print(f"Launched Bedrock {args.version} (PID {p.pid})")

def build_parser():
    p=argparse.ArgumentParser(prog="mclib",description="MCLI Bedrock — Minecraft for Windows CLI launcher")
    p.add_argument("--version",action="version",version="mclib 0.1.0")
    sub=p.add_subparsers(dest="command",required=True)

    s=sub.add_parser("versions")
    s.add_argument("--channel",choices=["release","preview"])
    s.add_argument("--arch",choices=["x64","x86","arm64","arm"])
    s.add_argument("--limit",type=int,default=50)
    s.set_defaults(func=cmd_versions)

    s=sub.add_parser("download")
    s.add_argument("version")
    s.add_argument("--arch",default="x64",choices=["x64","x86","arm64","arm"])
    s.add_argument("--channel",choices=["release","preview"])
    s.set_defaults(func=cmd_download)

    g=sub.add_parser("gdk")
    gs=g.add_subparsers(dest="gdk_action",required=True)
    x=gs.add_parser("import"); x.add_argument("version"); x.add_argument("directory"); x.set_defaults(func=cmd_gdk)
    x=gs.add_parser("list"); x.set_defaults(func=cmd_gdk)
    x=gs.add_parser("launch"); x.add_argument("version"); x.set_defaults(func=cmd_gdk)

    s=sub.add_parser("status"); s.set_defaults(func=cmd_status)
    s=sub.add_parser("packages"); s.set_defaults(func=cmd_packages)
    s=sub.add_parser("installed"); s.set_defaults(func=cmd_packages)

    s=sub.add_parser("launch")
    s.add_argument("--version",help="Installed Bedrock version/package substring")
    s.set_defaults(func=cmd_launch)

    s=sub.add_parser("install")
    s.add_argument("package",help="Bedrock version or local .msix/.appx/.msixbundle/.appxbundle path")
    s.add_argument("--arch",default="x64",choices=["x64","x86","arm64","arm"])
    s.add_argument("--channel",choices=["release","preview"])
    s.set_defaults(func=cmd_install)

    s=sub.add_parser("uninstall")
    s.add_argument("package_full_name")
    s.set_defaults(func=cmd_uninstall)
    return p

def main():
    try:
        args=build_parser().parse_args()
        args.func(args)
    except BedrockError as e:
        print(f"ERROR: {e}",file=sys.stderr)
        raise SystemExit(1)
