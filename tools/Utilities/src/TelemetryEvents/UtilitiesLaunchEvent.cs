// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Utilities.TelemetryEvents;

[EventData]
public class UtilitiesLaunchEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public enum Phase
    {
        Start,
        Error,
        Complete,
    }

    public string ActivityId { get; }

    public string UtilityName { get; }

    public bool LaunchedAsAdmin { get; }

    public Phase LaunchPhase { get; }

    public string ErrorString { get; }

    public UtilitiesLaunchEvent(Guid activityId, string utilityName, bool launchedAsAdmin, Phase phase, string errorString = "")
    {
        ActivityId = activityId.ToString();
        UtilityName = utilityName;
        LaunchedAsAdmin = launchedAsAdmin;
        LaunchPhase = phase;
        ErrorString = errorString;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
