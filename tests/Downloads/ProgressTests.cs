using System;
using System.ComponentModel;
using System.IO;
using BedrockLauncher;
using BedrockLauncher.Enums;
using BedrockLauncher.ViewModels;

static class ProgressTests
{
    internal static void Run()
    {
        var original = Console.Out;
        using var output = new StringWriter();
        Console.SetOut(output);
        try
        {
            var model = new ProgressBarModel();
            using (var bar = new ConsoleProgress(model))
            {
                model.CurrentState = LauncherState.isDownloading;
                model.Notify(nameof(model.CurrentState));
                model.ActualCurrentProgress = model.ActualTotalProgress = 1048576;
                model.Notify(nameof(model.TextualProgress));
                model.CurrentState = LauncherState.isExtracting;
                model.Notify(nameof(model.CurrentState));
                model.Notify(nameof(model.TextualProgress));
            }
            string text = output.ToString();
            if (!text.Contains("100.0%") || !text.Contains("1.0 / 1.0 MiB") || !text.Contains("Extracting ["))
                throw new Exception("Missing progress display.");
        }
        finally { Console.SetOut(original); }
    }
}

// Minimal event source isolates the actual console renderer from WPF.
namespace BedrockLauncher.ViewModels
{
    public class ProgressBarModel
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public LauncherState CurrentState { get; set; }
        public long ActualCurrentProgress { get; set; }
        public long ActualTotalProgress { get; set; }
        public string TextualProgress { get; set; }
        public void Notify(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
namespace BedrockLauncher
{
    internal static class Cli
    {
        internal static string GetStatusMessage(LauncherState state) => state.ToString();
    }
}
