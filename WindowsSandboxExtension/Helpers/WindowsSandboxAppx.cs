// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Security.Principal;
using Windows.Management.Deployment;

namespace WindowsSandboxExtension.Helpers;

internal sealed class WindowsSandboxAppx
{
    private const string PackageFamilyName = "Microsoft.Windows.WindowsSandbox";

    public static bool IsInstalled()
    {
        PackageManager packageManager = new PackageManager();

        var securityId = WindowsIdentity.GetCurrent().Owner?.ToString();
        var packages = packageManager.FindPackagesForUser(securityId, PackageFamilyName);

        return packages.Any();
    }
}
