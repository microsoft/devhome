// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.PI.TelemetryEvents;

[EventData]
public class TimedStopEventBase : EventBase
{
    internal TimedStopEventBase(string featureName, DateTime featureStopTime)
    {
        Name = featureName;
        StopTime = featureStopTime;
    }

    public string Name { get; private set; }

    public DateTime StopTime { get; private set; }

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
    }
}
