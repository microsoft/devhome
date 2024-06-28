// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace WSLDistributionLauncher.Models;

internal sealed class WslLauncher : WslLauncherBase
{
    public WslLauncher(string distributionName)
    {
        DistributionName = distributionName;
    }

    public WslLauncher(string distributionName, string commands)
    {
        DistributionName = distributionName;
        Commands = commands;
    }

    public override void Launch()
    {
        uint exitCode;
        var result = PInvoke.WslLaunchInteractive(DistributionName, Commands, UseCurrentWorkingDirectory, out exitCode);

        if (result.Failed)
        {
            throw new WslLaunchException($"WSL launch failed with Hresult: {result.Value:X}");
        }

        if (exitCode != 0)
        {
            throw new WslLaunchException($"WSL session exited with error code: {exitCode}");
        }
    }
}
