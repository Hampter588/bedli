# bedli

**bedli** is an unofficial command-line launcher for Minecraft Bedrock on Windows,
built from [BedrockLauncher (BL)](https://github.com/BedrockLauncher/BedrockLauncher).
Choose a release, beta, or preview version from your terminal. Bedli downloads and
installs it when needed, then attempts to launch that local version.

Downloads and extraction have progress bars. Large downloads use up to four
parallel connections when the server supports them, with a single-connection
fallback.

## Get started

1. Download the Windows x64 ZIP from [Releases](https://github.com/Hampter588/BEDLI/releases).
2. Extract the **entire ZIP** into a folder. Keep `bedli.exe` with its accompanying files.
3. Open a terminal in that folder and run:

   ```powershell
   .\bedli.exe help
   .\bedli.exe versions
   ```

The release ZIP includes the .NET runtime. Add the extracted folder to your
Windows `PATH` and open a new terminal to use `bedli` from any directory.

## Usage

```text
bedli {release,beta,preview} {version}
```

List the available versions first, then use the exact version shown for your
chosen channel:

```text
bedli versions
bedli release 1.16.1.2
bedli preview 1.21.100.20
bedli help
```

The version commands above are examples; availability depends on the catalog and
your machine's architecture. `bedli versions` groups entries by channel and shows
their architectures. `bedli help`, `bedli --help`, and `bedli -h` show the commands.

Bedli reuses cached downloads and extracted game files. If the requested version
cannot start, it reports an error instead of falling back to another installed
version. Exit codes are `0` for success, `1` for an operation failure, and `2` for
invalid arguments.

## Before playing

- You need a valid Minecraft license. Bedli does not provide one.
- Older UWP versions may require **Developer Mode** in Windows Settings.
- A Store-installed Minecraft package can block registration of an older local
  version with the same identity. Bedli does not automatically remove the Store
  installation. **Back up your worlds before replacing any installation.**
- Some versions may still fail to launch. A successful startup check does not
  guarantee the game will remain open or prevent the game itself from requesting updates.

## Credits

Bedli is based on **[BedrockLauncher](https://github.com/BedrockLauncher/BedrockLauncher)**.
Credit goes to the BL team and its contributors for the original launcher and
the download, installation, and version-management code this project builds on.
Bedli adapts that work into a CLI and adds terminal progress and parallel downloads.

Minecraft belongs to Mojang Studios and Microsoft. This project is not affiliated
with or endorsed by them.

See [LICENSE](LICENSE) for the project's GNU GPL v3 license.
