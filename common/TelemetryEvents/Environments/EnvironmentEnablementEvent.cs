// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using DevHome.Common.TelemetryEvents.SetupFlow.Environments;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.Environments;

public enum FeatureEnablementKind
{
    HyperVFeature,
    HyperVAdminGroup,
}

[EventData]
public class EnvironmentEnablementEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public string Features { get; }

    public string? FailureMessage { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentEnablementEvent"/> class.
    /// </summary>
    /// <param name="features">Dictionary of the enablement type and its status</param>
    /// <param name="failureMessage">Associated error text if it exists after an enablement attempt</param>
    public EnvironmentEnablementEvent(Dictionary<FeatureEnablementKind, EnvironmentsTelemetryStatus> features, string? failureMessage = null)
    {
        Features = ConvertFeaturesToString(features);
        FailureMessage = failureMessage;
    }

    // Inherited but unused.
    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
    }

    private string ConvertFeaturesToString(Dictionary<FeatureEnablementKind, EnvironmentsTelemetryStatus> features)
    {
        var featureList = new List<string>();
        foreach (var feature in features)
        {
            featureList.Add($"{feature.Key}:{feature.Value}");
        }

        return string.Join(',', featureList);
    }
}
