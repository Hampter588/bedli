# BEDLI

Minecraft Bedrock Edition from the command line.

BEDLI is a Windows x64 C#/.NET CLI for discovering, downloading, installing, listing, launching and removing managed Minecraft Bedrock versions.

## Commands

```powershell
bedli versions --arch x64
bedli download 26.51.1
bedli install 26.51.1
bedli installed
bedli launch 26.51.1
bedli remove 26.51.1
```

Downloads use 16 HTTP range workers when supported.

## Requirements

Windows 10/11 x64. GDK installs require Minecraft for Windows to be legitimately owned and installed through Microsoft Store/Xbox. BEDLI relies on standard Windows package and Gaming Services behavior.

## License and upstream

GPL-3.0. BEDLI is based on techniques from BedrockLauncher/BedrockLauncher and MCMrARM/mc-w10-version-launcher. Minecraft is a Microsoft trademark; BEDLI is independent and is not affiliated with Mojang or Microsoft.
