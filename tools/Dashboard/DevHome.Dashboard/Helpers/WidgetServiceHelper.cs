// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Linq;
using Windows.Management.Deployment;

namespace DevHome.Dashboard.Helpers;
internal class WidgetServiceHelper
{
    private readonly Version minSupportedVersion400 = new (423, 21300); // The next public version that has the update v423.21300.10.0
    private readonly Version minSupportedVersion500 = new (523, 17300);
    private readonly Version version500 = new (500, 0);

    private bool _validatedWebExpPack;

    public WidgetServiceHelper()
    {
        _validatedWebExpPack = false;
    }

    public bool EnsureWebExperiencePack()
    {
        // If already validated there's a good version, don't check again.
        if (_validatedWebExpPack)
        {
            return true;
        }

        // Ensure the application is installed, and the version is high enough.
        const string packageName = "MicrosoftWindows.Client.WebExperience_cw5n1h2txyewy";

        var packageManager = new PackageManager();
        var packages = packageManager.FindPackagesForUser(string.Empty, packageName);
        if (packages.Any())
        {
            // A user cannot actually have more than one version installed, so only need to look at the first result.
            var package = packages.First();

            var version = package.Id.Version;
            var major = version.Major;
            var minor = version.Minor;
            var build = version.Build;
            var revision = version.Revision;

            Log.Logger()?.ReportInfo("WidgetServiceHelper", $"{package.Id.FullName} Version: {major}.{minor}.{build}.{revision}");

            // Create System.Version type from PackageVersion to test. System.Version supports CompareTo() for easy comparisons.
            if (!IsVersionSupported(new (major, minor)))
            {
                return false;
            }
        }
        else
        {
            // If there is no version installed at all.
            return false;
        }

        _validatedWebExpPack = true;
        return _validatedWebExpPack;
    }

    /// <summary>
    /// Tests whether a version is equal to or above the min, but less than the max.
    /// </summary>
    private bool IsVersionBetween(Version target, Version min, Version max) =>
        target.CompareTo(min) >= 0 && target.CompareTo(max) < 0;

    /// <summary>
    /// Tests whether a version is equal to or above the min.
    /// </summary>
    private bool IsVersionAtOrAbove(Version target, Version min) => target.CompareTo(min) >= 0;

    private bool IsVersionSupported(Version target) =>
        IsVersionBetween(target, minSupportedVersion400, version500) || IsVersionAtOrAbove(target, minSupportedVersion500);
}
