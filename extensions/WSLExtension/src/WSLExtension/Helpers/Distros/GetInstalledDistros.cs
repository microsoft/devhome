// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WSLExtension.Models;
using WSLExtension.Services;

namespace WSLExtension.Helpers.Distros;

public class GetInstalledDistros
{
    public static List<Distro> Execute(IProcessCaller processCaller)
    {
        var distroListDetail = processCaller.CallProcess("wsl", "--list --verbose", out var exitCode);
        if (NoDistributionInstalled(exitCode, distroListDetail))
        {
            return new List<Distro>();
        }

        return WslCommandUtils.ParseDistroListDetail(distroListDetail);
    }

    private static bool NoDistributionInstalled(int exitCode, string commandOutput)
    {
        return !commandOutput.Contains("NAME")
               || commandOutput.Contains("Wsl/WSL_E_DEFAULT_DISTRO_NOT_FOUND")
               || exitCode != 0;
    }
}
