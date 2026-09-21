# BEDLI

**Minecraft Bedrock from your terminal.**

BEDLI is a separate Windows-only project inspired by MCLI's command style. It manages Minecraft for Windows packages and launches installed Bedrock versions from the command line.

> Early project. Bedrock package acquisition/registration varies by Windows version and Microsoft Store policy. Use versions you are legally entitled to access.

## Commands

```powershell
bedli status
bedli installed
bedli launch
bedli launch --version <version>
bedli install <path-to-msix-or-appx>
bedli uninstall <package-name>
bedli packages
```

## Install from source

```powershell
python -m pip install -e .
bedli --help
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

BEDLI follows the same public version sources used by BedrockLauncher:

- Rayth Network community version database for the historical Bedrock catalog.
- MinecraftBedrockArchiver/GdkLinks for direct modern GDK package links hosted on Microsoft's Xbox Live CDN.
- Installed Windows package registration for locally available Minecraft versions.

```powershell
bedli versions
bedli versions --channel release --arch x64
bedli download 26.1 --arch x64
```

Entries marked `direct` can be downloaded directly from Microsoft's package CDN. Entries marked `store` are historical catalog identities that require the Microsoft Store entitlement/update-link flow; BEDLI does not bypass Store ownership checks.

Downloaded packages are cached under `%USERPROFILE%\.bedli\downloads\`.

## GDK support

BEDLI is GPL-3.0. The native GDK helper is adapted from MCMrARM/mc-w10-version-launcher (GPL-3.0).

For GDK-era Bedrock, Windows must already have a legitimate Minecraft for Windows license/installation available. BEDLI does not bypass Microsoft licensing.

```powershell
bedli download 26.1 --arch x64
bedli gdk import 26.1 C:\path\to\extracted\minecraft
bedli gdk list
bedli gdk launch 26.1
```

The release ZIP includes `GDKDecryptHelper.exe`, built from the GPL source included under `native/GDKDecryptHelper`.
