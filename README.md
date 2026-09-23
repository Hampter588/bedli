# bedli

A Windows command-line Minecraft Bedrock launcher, based on BedrockLauncher.

```text
bedli {release,beta,preview} {version}
bedli release 1.21.100
bedli preview 1.21.100.20
bedli versions
bedli help
bedli --help
```

The command looks up the exact version for the selected channel and machine
architecture, installs it if necessary, and launches the game. It exits after
launching. Unavailable versions and installation failures are printed to stderr.
Exit codes: `0` success, `1` operation failed, `2` invalid arguments.

CLI launches require the requested local version folder. Bedli does not fall back
to a Store-managed installation or the generic `minecraft:` URI. It observes the
game process for five seconds before reporting success; an immediate exit,
including exit code 0, is an error. This is a startup check, not a guarantee that
Minecraft will remain running afterward or that the game itself cannot request updates.

`bedli versions` refreshes the existing catalog and lists all known versions,
grouped by release, beta, and preview, newest first within each channel.
Duplicate versions are combined with their available architectures. The list
includes local installations and omits the synthetic "Latest" entries.
`bedli help`, `--help`, and `-h` show usage without fetching the catalog.

Downloads and extraction show a terminal progress bar. Download progress includes
MiB transferred; redirected output prints periodic progress lines instead of
redrawing the terminal. Fresh downloads of at least 16 MiB use four parallel byte
ranges when the server provides range support and a strong ETag. If range responses
are unsupported or inconsistent, bedli falls back to a single connection. Existing
partial single-stream downloads retain their resumable download path.

Build on Windows with the .NET 8 SDK:

```powershell
git submodule update --init --recursive
dotnet build BedrockLauncher/BedrockLauncher.csproj -c Release
dotnet publish BedrockLauncher/BedrockLauncher.csproj -c Release -r win-x64 --self-contained true -o BedrockLauncher/bin/bedli
```

Run `bedli.exe` from `BedrockLauncher/bin/Release/net8.0-windows10.0.17763.0/`
or add that directory to PATH. Keep the executable alongside its build dependencies.
The self-contained publish command produces `BedrockLauncher/bin/bedli/bedli.exe`
with the runtime included. Add that folder to PATH to use `bedli` from any terminal.
Run the argument checks with `powershell -ExecutionPolicy Bypass -File tests/cli-smoke.ps1`.
The launcher displays no GUI; shared WPF backend dependencies are still included.
MSIXVC extraction creates an internal `LeviLauncher.exe` helper alongside `bedli.exe`,
because the bundled native extractor requires that process name. Continue using
`bedli` for all commands. The helper only runs the extraction operation.
Existing version caches and settings are reused. Locally registered UWP versions
use saves under the configured data directory at `bedli/saves/<channel>/<version>`;
the existing installer backs up previous non-linked save folders before redirecting.
GDK and externally installed versions retain their existing save behavior.
Minecraft ownership and the existing runtime requirements still apply. Beta downloads
that require authentication use the existing configured insider account; this CLI
does not add an account sign-in command.

Older UWP versions may require Developer Mode for local package registration.
When their manifest requires Microsoft.Services.Store.Engagement, the CLI installs
the existing x64 framework dependency automatically if a compatible version is missing.

## Publishing a release

In GitHub, open **Actions → Build and release bedli → Run workflow**. Select the
branch to build, enter a new tag such as `v1.0.3`, and optionally mark it as a
prerelease. The workflow runs tests, publishes a self-contained Windows x64 build,
and uploads `bedli-<tag>-win-x64.zip` and its SHA-256 checksum to GitHub Releases.
Extract the whole ZIP before running `bedli.exe`. It uses the built-in GitHub token;
no extra repository secret is required. Pushes and pull requests do not trigger it.

## Upstream project

---

An unofficial **Minecraft Bedrock** launcher that enables similar features to the **Minecraft Java Edition Launcher**.

[![Website](https://img.shields.io/github/v/tag/BedrockLauncher/BedrockLauncher.GitHub.io?color=blue&label=Visit%20Official%20Website&logo=github&style=for-the-badge)](https://bedrocklauncher.github.io/)
[![Download Release](https://img.shields.io/github/v/release/BedrockLauncher/BedrockLauncher?label=Download%20Release&logo=windows&sort=date&style=for-the-badge)](https://github.com/BedrockLauncher/BedrockLauncher/releases/latest/)
[![Crowdin](https://img.shields.io/static/v1?color=282C34&labelColor=282C34&label=Crowdin&message=Translate&logo=crowdin&style=for-the-badge)](https://crowdin.com/project/bedrocklauncher)

## Prerequisites
This launcher has both hardware and software prerequisites to ensure quality, performance, and stability:
- [Hardware prerequisites](./docs/HARDWARE_PREREQUISITES.md)
- [Software prerequisites](./docs/SOFTWARE_PREREQUISITES.md)

## Disclaimers
We specify disclaimers to ensure we go "told ya!" if anything happens:
- [Disclaimers](./docs/DISCLAIMERS.md)

## Get the launcher
Visit the official website to download the launcher
- [Official website](https://bedrocklauncher.github.io)

## Compiling the launcher yourself
You can compile the launcher yourself to prototype, iterate, or just enjoy the cutting-edge build:
- [Compiling instructions](./docs/COMPILING.md)

## Screenshots
Have a look around the launcher before you download on the official website:
- [Screenshots](https://bedrocklauncher.github.io)

## Credits
It's hard to keep the credits page updated on GitHub, so visit the credits page on the official website:
- [Credits page](https://bedrocklauncher.github.io/credits)
