// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Win32;
using static Microsoft.Win32.Registry;
using static WSLExtension.Constants;

namespace WSLExtension.Models;

public class DistributionState
{
    private const int InstallingState = 3;

    public string FriendlyName { get; set; } = string.Empty;

    public string DistributionName { get; set; } = null!;

    public bool? Version2 { get; set; }

    public string? SubKeyName { get; set; }

    public string? PackageFamilyName { get; set; }

    public string? Logo { get; set; }

    public DistributionState()
    {
    }

    public DistributionState(string registration)
    {
        DistributionName = registration;
    }

    public DistributionState(string? distributionName, string? subkeyName, string? packageFamilyName, bool isVersion2)
    {
        DistributionName = distributionName ?? "(Unknown)";
        SubKeyName = subkeyName;
        Version2 = isVersion2;
        PackageFamilyName = packageFamilyName;
    }

    public bool IsDistributionBeingInstalled()
    {
        var distributionKey = CurrentUser.OpenSubKey($@"{WslRegisryLocation}\{SubKeyName}", false);

        if (distributionKey == null)
        {
            return false;
        }

        var state = distributionKey?.GetValue(WslState) as int?;
        return state == InstallingState;
    }
}
