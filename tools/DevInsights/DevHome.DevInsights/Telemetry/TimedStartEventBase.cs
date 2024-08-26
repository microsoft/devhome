// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.DevInsights.TelemetryEvents;

[EventData]
public class TimedStartEventBase : EventBase
{
    protected TimedStartEventBase(string featureName, DateTime featureStartTime)
    {
        Name = featureName;
        StartTime = featureStartTime;
    }

    protected string Name { get; private set; }

    protected DateTime StartTime { get; private set; }

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
    }
}
