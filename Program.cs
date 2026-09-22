using Bedli;

try
{
    if (!OperatingSystem.IsWindows()) throw new BedliException("BEDLI supports Windows only.");
    await Cli.RunAsync(args);
}
catch (BedliException ex)
{
    Console.Error.WriteLine("ERROR: " + ex.Message);
    Environment.ExitCode = 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine("FATAL: " + ex);
    Environment.ExitCode = 1;
}
