// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using WSLExtension.Exceptions;
using WSLExtension.Services;

namespace WSLExtension.Helpers.Distros;

public class DistroState
{
    private static readonly Guid NamespaceGuid =
        new(0x2bde4a90, 0xd05f, 0x401c, 0x94, 0x92, 0xe4, 0x8, 0x84, 0xea, 0xd1, 0xd8);

    public static void Run(string registration, string? wtProfileGuid, bool isRoot, IProcessCaller processCaller)
    {
        var rootOption = isRoot ? "-u root" : string.Empty;

        const string loginCmd = "-- cd ~ && $(getent passwd $LOGNAME | cut -d: -f7) --login";
        try
        {
            var profile = wtProfileGuid ?? ComputeProfileGuid(registration);

            processCaller.CallDetachedProcess(
        "wt",
        $"--profile {profile} -- wsl -d {registration} {rootOption} {loginCmd}");
        }
        catch (Exception)
        {
            processCaller.CallDetachedProcess(
                "wsl",
                $"-d {registration} {rootOption} {loginCmd}");
        }
    }

    public static void Terminate(string registration, IProcessCaller processCaller)
    {
        processCaller.CallProcess(
            "wsl",
            $"--terminate {registration}",
            exitCode: out var exitCode);

        if (exitCode != 0)
        {
            throw new WslManagerException($"Failed to terminate the distro {registration}");
        }
    }

    public static string ComputeProfileGuid(string name)
    {
        var encoding = new UnicodeEncoding(false, false);

        var guid = GuidUtility.Create(NamespaceGuid, encoding.GetBytes(name), 5);

        return $"{{{guid}}}";
    }
}
