using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BedrockLauncher.Backend.Backporting;
using BedrockLauncher.Classes;
using BedrockLauncher.Enums;
using BedrockLauncher.UpdateProcessor.Enums;
using BedrockLauncher.UpdateProcessor.Extensions;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher
{
    internal sealed class Cli : IBackwardsCommunication
    {
        private const string Usage = "Usage: bedli {release,beta,preview} {version}\n       bedli versions\n       bedli help";

        internal static int Run(string[] args)
        {
            if (args.Length == 1 && (string.Equals(args[0], "help", StringComparison.OrdinalIgnoreCase) || args[0] == "--help" || args[0] == "-h"))
            {
                Console.WriteLine(Usage);
                Console.WriteLine("Installs the requested version if needed, then launches Minecraft.");
                Console.WriteLine("versions  Lists all known versions by channel, newest first, with available architectures.");
                Console.WriteLine("help      Shows this help (also --help or -h).");
                Console.WriteLine("Example: bedli release 1.21.100");
                return 0;
            }

            bool listVersions = args.Length == 1 && string.Equals(args[0], "versions", StringComparison.OrdinalIgnoreCase);
            if (!listVersions && (args.Length != 2 ||
                !new[] { "release", "beta", "preview" }.Contains(args[0].ToLowerInvariant()) ||
                !System.Text.RegularExpressions.Regex.IsMatch(args[1], @"^\d+\.\d+\.\d+(\.\d+)?$") ||
                !Version.TryParse(args[1], out _)))
            {
                Console.Error.WriteLine(Usage);
                Console.Error.WriteLine("Use 'bedli help' for commands and examples.");
                return 2;
            }

            // The existing backend uses a dispatcher. No App, XAML startup, or window is created.
            var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            MainDataModel.SetBackwardsCommunicationHost(new Cli());
            application.Resources["EditInstallationScreen_LatestRelease"] = "Latest release";
            application.Resources["EditInstallationScreen_LatestBeta"] = "Latest beta";
            application.Resources["EditInstallationScreen_LatestPreview"] = "Latest preview";
            int exitCode = 1;
            application.Startup += async (_, __) =>
            {
                try
                {
                    if (listVersions) await ListVersionsAsync();
                    else await LaunchAsync(Enum.Parse<VersionType>(args[0], true), args[1]);
                    exitCode = 0;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("bedli: " + ex.GetBaseException().Message);
                }
                finally
                {
                    application.Shutdown(exitCode);
                }
            };
            application.Run();
            return exitCode;
        }

        private static async Task ListVersionsAsync()
        {
            var model = MainDataModel.Default;
            Console.Error.WriteLine("Loading available versions...");
            await model.PackageManager.VersionDownloader.UpdateVersionList(model.Versions);
            var versions = model.Versions
                .Where(v => Version.TryParse(v.Name, out _))
                .GroupBy(v => new { v.Type, v.Name })
                .OrderBy(group => group.Key.Type)
                .ThenByDescending(group => Version.Parse(group.Key.Name))
                .ToList();

            Console.WriteLine("CHANNEL  VERSION           ARCHITECTURES");
            foreach (var group in versions)
            {
                string architectures = string.Join(", ", group.Select(v => v.Architecture)
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value));
                Console.WriteLine($"{group.Key.Type.ToString().ToLowerInvariant(),-8} {group.Key.Name,-17} {architectures}");
            }
            if (versions.Count == 0) Console.WriteLine("No versions available in the catalog or local installations.");
        }

        private static async Task LaunchAsync(VersionType channel, string name)
        {
            var model = MainDataModel.Default;
            var manager = model.PackageManager;
            Console.WriteLine("Loading available versions...");
            await manager.VersionDownloader.UpdateVersionList(model.Versions);
            var version = model.Versions
                .Where(v => v.Type == channel && v.Name == name &&
                    VersionDbExtensions.DoesVerionArchMatch(Constants.CurrentArchitecture, v.Architecture))
                .OrderByDescending(v => v.IsInstalledInLauncher)
                .FirstOrDefault();
            if (version == null)
                throw new InvalidOperationException($"{channel.ToString().ToLowerInvariant()} {name} is unavailable for {Constants.CurrentArchitecture}.");

            string savePath = Path.Combine(model.FilePaths.CurrentLocation, "bedli", "saves", channel.ToString().ToLowerInvariant(), name);
            using var progress = new ConsoleProgress(model.ProgressBarState);
            Console.WriteLine($"Preparing {channel.ToString().ToLowerInvariant()} {name}...");
            await manager.InstallPackage(version, savePath);
            MCVersion.ClearInstallProbeCache();
            if (!version.IsInstalled)
                throw new InvalidOperationException("Installation did not produce a playable version.");
            await manager.LaunchPackage(version, savePath, false, false);
            Console.WriteLine($"Minecraft {name} is running from its local version folder.");
        }

        internal static string GetStatusMessage(LauncherState state) => state switch
        {
            LauncherState.isInitializing => "Checking installation...",
            LauncherState.isDownloading => "Downloading Minecraft...",
            LauncherState.isExtracting => "Extracting game files...",
            LauncherState.isLaunching => "Launching Minecraft...",
            LauncherState.isRegisteringPackage => "Registering Minecraft with Windows...",
            LauncherState.isRemovingPackage => "Unregistering the previous Minecraft package...",
            LauncherState.isUninstalling => "Removing game files...",
            LauncherState.isBackingUp => "Backing up game files...",
            LauncherState.isCanceling => "Canceling...",
            _ => null
        };

        public DependencyObject ProgressBarGrid => null;
        public void UpdateAnimatePageTransitions(bool value) { }
        public void errormsg(string title, string text, Exception exception) => throw exception;
        public Task<bool> exceptionmsg(Exception exception) => Task.FromException<bool>(exception);
        public Task<System.Windows.Forms.DialogResult> ShowDialog_YesNo(string title, string content)
        {
            Console.Error.WriteLine($"{title}: {content}");
            return Task.FromResult(System.Windows.Forms.DialogResult.No);
        }
    }
}
