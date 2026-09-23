using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BedrockLauncher.Handlers
{
    internal static class LaunchProbe
    {
        internal static async Task<bool> StaysRunningAsync(Process process, TimeSpan observationTime)
        {
            Task exited = process.WaitForExitAsync();
            return await Task.WhenAny(exited, Task.Delay(observationTime)) != exited && !process.HasExited;
        }
    }
}
