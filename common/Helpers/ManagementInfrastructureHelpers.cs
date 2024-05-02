// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Management.Infrastructure;
using Serilog;

namespace DevHome.Common.Helpers;

public enum FeatureAvailabilityKind
{
    NotPresent,
    Enabled,
    Disabled,
}

public static class ManagementInfrastructureHelpers
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(ManagementInfrastructureHelpers));

    public static FeatureAvailabilityKind IsWindowsFeatureAvailable(string featureName)
    {
        try
        {
            // use the local session
            using var session = CimSession.Create(null);

            // There will only be one feature returned by the query
            foreach (var featureInstance in session.QueryInstances("root\\cimv2", "WQL", $"SELECT * FROM Win32_OptionalFeature WHERE Name = '{featureName}'"))
            {
                var installState = featureInstance?.CimInstanceProperties["InstallState"].Value;
                if (installState != null)
                {
                    var enablementState = Convert.ToBoolean(installState, CultureInfo.InvariantCulture) ? FeatureAvailabilityKind.Enabled : FeatureAvailabilityKind.Disabled;

                    _log.Information($"Found feature: '{featureName}' with enablement state: '{enablementState}'");
                    return enablementState;
                }
            }
        }
        catch (CimException ex)
        {
            // We'll handle cases where there are exceptions as if the feature does not exist.
            _log.Error(ex, $"Error attempting to get the {featureName} feature state");
        }

        _log.Information($"Unable to find {featureName} feature");
        return FeatureAvailabilityKind.NotPresent;
    }
}
