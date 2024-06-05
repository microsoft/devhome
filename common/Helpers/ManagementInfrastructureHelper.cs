// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Management.Infrastructure;
using Serilog;

namespace DevHome.Common.Helpers;

// States based on InstallState value in Win32_OptionalFeature
// See: https://learn.microsoft.com/windows/win32/cimwin32prov/win32-optionalfeature
public enum FeatureAvailabilityKind
{
    Enabled,
    Disabled,
    Absent,
    Unknown,
}

public static class ManagementInfrastructureHelper
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(ManagementInfrastructureHelper));

    public static FeatureAvailabilityKind IsWindowsFeatureAvailable(string featureName)
    {
        try
        {
            // use the local session
            using var session = CimSession.Create(null);

            // There will only be one feature returned by the query
            foreach (var featureInstance in session.QueryInstances("root\\cimv2", "WQL", $"SELECT * FROM Win32_OptionalFeature WHERE Name = '{featureName}'"))
            {
                if (featureInstance?.CimInstanceProperties["InstallState"].Value is uint installState)
                {
                    var featureAvailability = GetAvailabilityKindFromState(installState);

                    _log.Information($"Found feature: '{featureName}' with enablement state: '{featureAvailability}'");
                    return featureAvailability;
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error attempting to get the {featureName} feature state");
        }

        _log.Information($"Unable to get state of {featureName} feature");
        return FeatureAvailabilityKind.Unknown;
    }

    private static FeatureAvailabilityKind GetAvailabilityKindFromState(uint state)
    {
        switch (state)
        {
            case 1:
                return FeatureAvailabilityKind.Enabled;
            case 2:
                return FeatureAvailabilityKind.Disabled;
            case 3:
                return FeatureAvailabilityKind.Absent;
            default:
                return FeatureAvailabilityKind.Unknown;
        }
    }
}
