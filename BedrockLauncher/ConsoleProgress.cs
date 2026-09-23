using System;
using System.ComponentModel;
using System.Diagnostics;
using BedrockLauncher.Enums;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher
{
    internal sealed class ConsoleProgress : IDisposable
    {
        private readonly ProgressBarModel model;
        private readonly object gate = new object();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private LauncherState lastState;
        private long lastRender;
        private int lineWidth;

        internal ConsoleProgress(ProgressBarModel model)
        {
            this.model = model;
            model.PropertyChanged += Changed;
        }

        private void Changed(object sender, PropertyChangedEventArgs e)
        {
            lock (gate)
            {
                if (e.PropertyName == nameof(ProgressBarModel.CurrentState))
                {
                    if (lastState == model.CurrentState) return;
                    FinishLine();
                    lastState = model.CurrentState;
                    lastRender = 0;
                    string message = Cli.GetStatusMessage(lastState);
                    if (message != null) Console.WriteLine(message);
                }
                if (e.PropertyName != nameof(ProgressBarModel.TextualProgress) ||
                    (lastState != LauncherState.isDownloading && lastState != LauncherState.isExtracting)) return;
                long current = model.ActualCurrentProgress;
                long total = model.ActualTotalProgress;
                if (current <= 0) return;
                long interval = Console.IsOutputRedirected ? 2000 : 100;
                if (current != total && clock.ElapsedMilliseconds - lastRender < interval) return;
                lastRender = clock.ElapsedMilliseconds;
                string label = lastState == LauncherState.isDownloading ? "Downloading" : "Extracting";
                double fraction = total > 0 ? Math.Clamp((double)current / total, 0, 1) : 0;
                int filled = (int)(fraction * 20);
                string bar = new string('#', filled) + new string('-', 20 - filled);
                string amount = lastState == LauncherState.isDownloading
                    ? $"{current / 1048576d:0.0} / {(total > 0 ? (total / 1048576d).ToString("0.0") : "?")} MiB"
                    : $"{current:N0} / {(total > 0 ? total.ToString("N0") : "?")}";
                string percentage = total > 0 ? $"{fraction * 100,5:0.0}%" : "   ? %";
                string line = $"{label} [{bar}] {percentage}  {amount}";
                if (Console.IsOutputRedirected) Console.WriteLine(line);
                else
                {
                    int width = Math.Max(1, Console.WindowWidth - 1);
                    if (line.Length > width) line = line.Substring(0, width);
                    Console.Write("\r" + line.PadRight(Math.Min(width, Math.Max(lineWidth, line.Length))));
                    lineWidth = line.Length;
                }
            }
        }

        private void FinishLine()
        {
            if (lineWidth > 0) Console.WriteLine();
            lineWidth = 0;
        }

        public void Dispose()
        {
            model.PropertyChanged -= Changed;
            lock (gate) FinishLine();
        }
    }
}
