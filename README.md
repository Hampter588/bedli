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
