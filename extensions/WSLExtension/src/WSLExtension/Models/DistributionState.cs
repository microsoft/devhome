// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Win32;
using WSLExtension.DistroDefinitions;
using static Microsoft.Win32.Registry;
using static WSLExtension.Constants;

namespace WSLExtension.Models;

public class DistributionState
{
    private const int InstalledState = 1;

    public string FriendlyName { get; set; } = string.Empty;

    public string DistributionName { get; set; } = string.Empty;

    public bool? Version2 { get; set; }

    public string? SubKeyName { get; set; }

    public string? PackageFamilyName { get; set; }

    public string? LogoPathInPackage { get; set; }

    public string? Base64StringLogo { get; set; }

    public bool IsRunning { get; set; }

    public bool IsDefaultDistribution { get; set; }

    public DistributionState(string distributionName)
    {
        DistributionName = distributionName;
    }

    public DistributionState(KnownDistributionInfo knownDistributionInfo)
    {
        DistributionName = knownDistributionInfo.DistributionName;
        FriendlyName = knownDistributionInfo.FriendlyName;
        Base64StringLogo = knownDistributionInfo.Base64StringLogo;
    }

    public DistributionState(string distributionName, string base64StringForLogo)
    {
        DistributionName = distributionName;
        Base64StringLogo = base64StringForLogo;
    }

    public DistributionState(string? distributionName, string? subkeyName, string? packageFamilyName, bool isVersion2)
    {
        DistributionName = distributionName ?? "(Unknown)";
        SubKeyName = subkeyName;
        Version2 = isVersion2;
        PackageFamilyName = packageFamilyName;
    }

    public bool IsDistributionInstalled()
    {
        var distributionKey = CurrentUser.OpenSubKey($@"{WslRegisryLocation}\{SubKeyName}", false);

        if (distributionKey == null)
        {
            return false;
        }

        var state = distributionKey?.GetValue(WslState) as int?;

        // Any other state other than a 1 means the distribution is not fully installed yet.
        return state == InstalledState;
    }
}
