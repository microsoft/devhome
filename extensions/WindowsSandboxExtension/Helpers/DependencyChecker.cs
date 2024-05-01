// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Windows.Management.Deployment;
using WinRT;

namespace WindowsSandboxExtension.Helpers;

internal sealed class DependencyChecker
{
    private const string OptionalComponentName = "Containers-DisposableClientVM";
    private const string PackageFamilyName = "MicrosoftWindows.WindowsSandbox_cw5n1h2txyewy";

    public static bool IsOptionalComponentEnabled()
    {
        var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_OptionalFeature WHERE Name = '{OptionalComponentName}'");
        var collection = searcher.Get();

        foreach (ManagementObject instance in collection)
        {
            if (instance["InstallState"] != null)
            {
                var state = Convert.ToInt32(instance.GetPropertyValue("InstallState"), CultureInfo.InvariantCulture);

                // 1 means the feature is enabled
                return state == 1;
            }
        }

        // Return false if the feature is not found
        return false;
    }

    public static bool IsNewWindowsSandboxExtensionInstalled()
    {
        PackageManager packageManager = new PackageManager();

        var securityId = WindowsIdentity.GetCurrent().Owner?.ToString();
        var packages = packageManager.FindPackagesForUser(securityId, PackageFamilyName);

        return packages.Any();
    }
}
