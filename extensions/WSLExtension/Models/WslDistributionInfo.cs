// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WSLExtension.DistributionDefinitions;
using static Microsoft.Win32.Registry;
using static WSLExtension.Constants;

namespace WSLExtension.Models;

public class WslDistributionInfo
{
    private const int InstalledState = 1;

    public string FriendlyName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool? Version2 { get; set; }

    public string? SubKeyName { get; set; }

    public string? PackageFamilyName { get; set; }

    public string? Base64StringLogo { get; set; }

    public bool IsRunning { get; set; }

    public bool IsDefaultDistribution { get; set; }

    public bool UsePackageFamilyLogoOnEnvironmentsPage { get; set; } = true;

    public WslDistributionInfo(string distributionName)
    {
        Name = distributionName;
    }

    public WslDistributionInfo(DistributionDefinition distributionDistribution)
    {
        Name = distributionDistribution.Name;
        FriendlyName = distributionDistribution.FriendlyName;
        Base64StringLogo = distributionDistribution.Base64StringLogo;
    }

    public WslDistributionInfo(string? distributionName, string? subkeyName, string? packageFamilyName, bool isVersion2)
    {
        Name = distributionName ?? string.Empty;
        FriendlyName = Name;
        SubKeyName = subkeyName;
        Version2 = isVersion2;
        PackageFamilyName = packageFamilyName;
    }

    public bool IsDistributionInstalled()
    {
        var distributionKey = CurrentUser.OpenSubKey($@"{WslRegistryLocation}\{SubKeyName}", false);

        if (distributionKey == null)
        {
            return false;
        }

        var state = distributionKey?.GetValue(WslState) as int?;

        // Any other state other than a 1 means the distribution is not fully installed yet.
        return state == InstalledState;
    }
}
