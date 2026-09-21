# MCLI Bedrock

**Minecraft Bedrock from your terminal.**

MCLI Bedrock is a separate Windows-only project inspired by MCLI's command style. It manages Minecraft for Windows packages and launches installed Bedrock versions from the command line.

> Early project. Bedrock package acquisition/registration varies by Windows version and Microsoft Store policy. Use versions you are legally entitled to access.

## Commands

```powershell
mclib status
mclib installed
mclib launch
mclib launch --version <version>
mclib install <path-to-msix-or-appx>
mclib uninstall <package-name>
mclib packages
```

## Install from source

```powershell
python -m pip install -e .
mclib --help
```

## Scope

- Windows only
- discovers installed Minecraft for Windows packages
- lists package identity/version/install location
- launches installed Bedrock packages through their AppUserModelId
- installs local `.msix`, `.msixbundle`, `.appx`, or `.appxbundle` packages using Windows package deployment
- removes installed Bedrock packages by package full name
- keeps package discovery/launch logic isolated from MCLI Java

This project does **not** bypass Microsoft Store ownership/licensing and does not include Microsoft authentication tokens or private Store APIs.

## BedrockLauncher-compatible version sources

MCLI Bedrock follows the same public version sources used by BedrockLauncher:

- Rayth Network community version database for the historical Bedrock catalog.
- MinecraftBedrockArchiver/GdkLinks for direct modern GDK package links hosted on Microsoft's Xbox Live CDN.
- Installed Windows package registration for locally available Minecraft versions.

```powershell
mclib versions
mclib versions --channel release --arch x64
mclib download 26.1 --arch x64
```

Entries marked `direct` can be downloaded directly from Microsoft's package CDN. Entries marked `store` are historical catalog identities that require the Microsoft Store entitlement/update-link flow; MCLI Bedrock does not bypass Store ownership checks.

Downloaded packages are cached under `%USERPROFILE%\.mclib\downloads\`.
