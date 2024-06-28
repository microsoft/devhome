// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WSLDistributionLauncher.Models;

namespace WSLDistributionLauncher.Helpers;

internal sealed class WslLauncherFactory
{
    public WslLauncherBase GetLauncher(string[] args)
    {
        if (args.Length == 3 && args[1] == "-DistributionName")
        {
            return new WslLauncher(args[2]);
        }
        else if (args.Length == 5 && args[1] == "-DistributionName" && args[3] == "-Commands")
        {
            return new WslLauncher(args[2], args[4]);
        }

        throw new WslLaunchException($"Command line parameters not supported parameters: [{string.Join(", ", args)}]");
    }
}
