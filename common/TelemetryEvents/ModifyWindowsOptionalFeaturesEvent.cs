// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;
using static DevHome.Common.Scripts.ModifyWindowsOptionalFeatures;

namespace DevHome.Common.TelemetryEvents;

[EventData]
public class ModifyWindowsOptionalFeaturesEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public string FeaturesString
    {
        get;
    }

    public string ExitCode
    {
        get;
    }

    public long DurationMs
    {
        get;
    }

    public ModifyWindowsOptionalFeaturesEvent(string featureString, ExitCode result, long durationMs)
    {
        FeaturesString = featureString;
        ExitCode = GetExitCodeDescription(result);
        DurationMs = durationMs;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }

    public static void Log(string featureString, ExitCode result, long durationMs)
    {
        TelemetryFactory.Get<ITelemetry>().Log(
            "ModifyVirtualizationFeatures_Event",
            LogLevel.Measure,
            new ModifyWindowsOptionalFeaturesEvent(featureString, result, durationMs));
    }
}
