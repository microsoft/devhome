// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WSLExtension.DistributionDefinitions;
using static Microsoft.Win32.Registry;
using static WSLExtension.Constants;

namespace WSLExtension.Models;

/// <summary>
/// Represents information about a registered WSL distribution
/// </summary>
public class WslRegisteredDistribution
{
    private const int InstalledState = 1;

    public string FriendlyName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool? Version2 { get; set; }

    public string? SubKeyName { get; set; }

    public string? PackageFamilyName { get; set; }

    public string? Base64StringLogo { get; set; }

    public bool IsDefaultDistribution { get; set; }

    public string Publisher { get; set; } = string.Empty;

    public string? AssociatedTerminalProfileGuid { get; set; } = string.Empty;

    public WslRegisteredDistribution(string distributionName)
    {
        Name = distributionName;
    }

    public WslRegisteredDistribution(DistributionDefinition distributionDistribution)
    {
        Name = distributionDistribution.Name;
        FriendlyName = distributionDistribution.FriendlyName;
        Base64StringLogo = distributionDistribution.Base64StringLogo;
        AssociatedTerminalProfileGuid = distributionDistribution.WindowsTerminalProfileGuid;
    }

    public WslRegisteredDistribution(string distributionName, string? subkeyName, string? packageFamilyName, bool isVersion2)
    {
        Name = distributionName;
        FriendlyName = Name;
        SubKeyName = subkeyName;
        Version2 = isVersion2;
        PackageFamilyName = packageFamilyName;
    }

    /// <summary>
    /// Uses the registry information about the distribution to determine if its fully registered or not.
    /// </summary>
    /// <returns>True only when distribution is fully registered. False otherwise.</returns>
    public virtual bool IsDistributionFullyRegistered()
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
