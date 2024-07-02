// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace WSLDistributionLauncher.Models;

public class WslLauncher
{
    public const int InvalidArgsHResult = unchecked((int)0x80070057);

    private const bool UseCurrentWorkingDirectory = false;

    private readonly string _distributionName = string.Empty;

    private readonly string _commandsToRunInDistribution = string.Empty;

    private WslLauncher()
    {
    }

    private WslLauncher(string distributionName)
    {
        _distributionName = distributionName;
    }

    private WslLauncher(string distributionName, string commands)
    {
        _distributionName = distributionName;
        _commandsToRunInDistribution = commands;
    }

    public static WslLauncher CreateLauncher(string[] args)
    {
        if (args.Length == 2 && args[0] == "-DistributionName")
        {
            // -DistributionName <distribution name>
            return new WslLauncher(args[1]);
        }
        else if (args.Length == 4 && args[0] == "-DistributionName" && args[2] == "-Commands")
        {
            // -DistributionName <distribution name> -Commands "<command to run in distribution>"
            return new WslLauncher(args[2], args[3]);
        }

        throw new Win32Exception(InvalidArgsHResult, $"Command line parameters not supported parameters: [{string.Join(", ", args)}]");
    }

    public uint Launch()
    {
        uint exitCode;
        var result = PInvoke.WslLaunchInteractive(_distributionName, _commandsToRunInDistribution, UseCurrentWorkingDirectory, out exitCode);

        if (result.Failed)
        {
            throw new Win32Exception(result.Value, $"WSL launch failed with Hresult: {result.Value:X}");
        }

        return exitCode;
    }
}
