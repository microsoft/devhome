// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics.Tracing;
using DevHome.Logging;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.TelemetryEvents;

[EventData]
public class DevHomeClosedEvent : EventBase
{
    public double ElapsedTime
    {
        get;
    }

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public DevHomeClosedEvent(DateTime startTime)
    {
        ElapsedTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
        GlobalLog.Logger?.ReportDebug($"DevHome Closed Event, ElapsedTime: {ElapsedTime}ms");
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
