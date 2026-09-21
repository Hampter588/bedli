import argparse, json, platform, sys
from .windows import BedrockError, packages, launch, install, uninstall

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
    path=install(args.package)
    print("Installed package:",path)

def cmd_uninstall(args):
    require_windows()
    uninstall(args.package_full_name)
    print("Removed:",args.package_full_name)

def build_parser():
    p=argparse.ArgumentParser(prog="mclib",description="MCLI Bedrock — Minecraft for Windows CLI launcher")
    p.add_argument("--version",action="version",version="mclib 0.1.0")
    sub=p.add_subparsers(dest="command",required=True)

    s=sub.add_parser("status"); s.set_defaults(func=cmd_status)
    s=sub.add_parser("packages"); s.set_defaults(func=cmd_packages)
    s=sub.add_parser("installed"); s.set_defaults(func=cmd_packages)

    s=sub.add_parser("launch")
    s.add_argument("--version",help="Installed Bedrock version/package substring")
    s.set_defaults(func=cmd_launch)

    s=sub.add_parser("install")
    s.add_argument("package",help="Local .msix/.appx/.msixbundle/.appxbundle path")
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
