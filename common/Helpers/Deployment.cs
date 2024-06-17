// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Serilog;
using Windows.Storage;

namespace DevHome.Common.Helpers;

public static class Deployment
{
    private static readonly string _deploymentIdentifierKeyName = "DevHomeDeploymentIdentifier";

    // This creates and returns a Guid associated with this deployment of DevHome. This uniquely
    // identifies this deployment across multiple launches and will be different per Windows user.
    // This will persist across updates, but will be removed upon package removal, or when
    // ApplicationData is reset via settings. The purpose of this identifier is to correlate
    // telemetry events across multiple launches for product usage metrics.
    public static Guid Identifier
    {
        get
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings.Values.TryGetValue(_deploymentIdentifierKeyName, out var value))
                {
                    return (Guid)value;
                }

                var newGuid = Guid.NewGuid();
                localSettings.Values[_deploymentIdentifierKeyName] = newGuid;
                return newGuid;
            }
            catch (Exception ex)
            {
                // We do not want this identifier's access to ever create a problem in the
                // application, so if we can't get it, return empty guid. An empty guid is also a
                // signal that the data is unknown for filtering purposes.
                Log.Error(ex, $"Failed getting Deployment Identifier");
                return Guid.Empty;
            }
        }
    }
}
