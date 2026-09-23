using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BedrockLauncher.Handlers;

if (args.Length > 0)
{
    if (args[0] == "stay") await Task.Delay(2000);
    return args[0] == "fail" ? 7 : 0;
}

foreach (string mode in new[] { "exit", "fail", "stay" })
{
    using Process child = Process.Start(new ProcessStartInfo(Environment.ProcessPath)
    {
        UseShellExecute = false,
        CreateNoWindow = true,
        ArgumentList = { mode }
    });
    bool running = await LaunchProbe.StaysRunningAsync(child, TimeSpan.FromMilliseconds(500));
    if (running != (mode == "stay"))
        throw new Exception($"Startup check failed for {mode}.");
    await child.WaitForExitAsync();
}
Console.WriteLine("Passed startup checks: early exit 0, early exit 7, and a running process.");
return 0;
