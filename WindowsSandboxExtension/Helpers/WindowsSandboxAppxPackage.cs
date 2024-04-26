// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Management.Deployment;

namespace WindowsSandboxExtension.Helpers;

internal sealed class WindowsSandboxAppxPackage
{
    private const string PackageFamilyName = "MicrosoftWindows.WindowsSandbox";

    public static bool IsInstalled()
    {
        var packageManager = new PackageManager();
        var package = packageManager.FindPackages(PackageFamilyName);

        return package.Any();
    }
}
