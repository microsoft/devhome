// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WSLDistributionLauncher.Models;

namespace WSLDistributionLauncher;

/// <summary>
/// Simple console app to launch the WSL distribution inside of it. Doing it this way allows
/// us to know when the user exits the distribution. Unfortunately when launching wsl.exe directly
/// to launch into a distribution it opens the distribution then exits immediately after. So,
/// monitoring the exit of it does not tell us if the user exited the distribution on the commandline.
/// This launcher is different because we do not use piping and when we call WslLaunchInteractive it
/// takes over the command line and blocks execution. When WslLaunchInteractive exits we get control
/// back in the main method below and then return an exit code. The WSLExtension subscribes to the processes
/// exit event before starting it, so it will receive an event when this exe exits. This is how it will know
/// the user exited out of the distribution.
/// </summary>
public class LauncherProgram
{
#if Dev
    private const bool IsInDebugMode = true;
#else
    private const bool IsInDebugMode = false;
#endif
    private const string WslLogSubFolder = @"Logs\WSL";

    private const string LogName = "WSLDistributionLauncher";

    public static int Main(string[] args)
    {
        WriteToConsoleIfDebug($"Launched with args: {string.Join(' ', args.ToArray())}");

        try
        {
            var exitCode = WslLauncher.CreateLauncher(args).Launch();
            WriteToConsoleIfDebug($"WslLaunchInteractive API exited with exit code: {exitCode}");
            return unchecked((int)exitCode);
        }
        catch (Exception ex)
        {
            WriteToConsoleIfDebug($"Error launching WSL process due to exception: {ex}");
            return ex.HResult;
        }
    }

    private static void WriteToConsoleIfDebug(string message)
    {
        if (IsInDebugMode)
        {
            Console.WriteLine(message);
        }
    }
}
