// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using DevHome.Common.Models;
using Serilog;

namespace DevHome.Common.Scripts;

public static class ModifyLongPathsSetting
{
    public static ExitCode ModifyLongPaths(bool enabled, ILogger? log = null)
    {
        var scriptString = enabled ? EnableScript : DisableScript;
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -Command {scriptString}",
                UseShellExecute = true,
                Verb = "runas",
            },
        };

        try
        {
            process.Start();
            process.WaitForExit();

            return FromExitCode(process.ExitCode);
        }
        catch (Exception ex)
        {
            log?.Error(ex, "Failed to modify Long Paths setting");
            return ExitCode.Failure;
        }
    }

    public enum ExitCode
    {
        Success = 0,
        Failure = 1,
    }

    private static ExitCode FromExitCode(int exitCode)
    {
        return exitCode switch
        {
            0 => ExitCode.Success,
            _ => ExitCode.Failure,
        };
    }

    private const string EnableScript =
@"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem' -Name 'LongPathsEnabled' -Value 1
if ($?) { exit 0 } else { exit 1 }
";

    private const string DisableScript =
@"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem' -Name 'LongPathsEnabled' -Value 0
if ($?) { exit 0 } else { exit 1 }
";
}
