// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSLExtension.Exceptions;
using WSLExtension.Services;

namespace WSLExtension.Helpers.Distros;

public class Management
{
    public static void Unregister(string registration, IProcessCaller processCaller)
    {
        processCaller.CallProcess(
            "wsl",
            $"--unregister {registration}",
            exitCode: out var exitCode);

        if (exitCode != 0)
        {
            throw new WslManagerException($"Failed to unregister the distro {registration}");
        }
    }

    public static async Task<int> InstallWsl(IProcessCaller processCaller, string distroRegistration)
    {
        var exitCode = await processCaller.CallInteractiveProcess(
            "wsl",
            $"--install --distribution {distroRegistration}".Trim());

        if (exitCode == 0)
        {
            processCaller.CallProcess("shutdown.exe", "/r", Encoding.Unicode, out exitCode);
        }

        return exitCode;
    }

    public static void InstallDistro(IProcessCaller processCaller, string distroRegistration)
    {
        processCaller.CallDetachedProcess(
            "wsl",
            $"--install --distribution {distroRegistration}".Trim());
    }
}
