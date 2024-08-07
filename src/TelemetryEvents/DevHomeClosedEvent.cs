// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.Tracing;
using DevHome.Common.Helpers;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;
using Serilog;

namespace DevHome.TelemetryEvents;

[EventData]
public class DevHomeClosedEvent : EventBase
{
    public double ElapsedTime { get; }

    public Guid DeploymentIdentifier { get; }

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public DevHomeClosedEvent(DateTime startTime)
    {
        ElapsedTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
        DeploymentIdentifier = Deployment.Identifier;
        var log = Log.ForContext("SourceContext", nameof(DevHomeClosedEvent));
        log.Debug($"DevHome Closed Event, ElapsedTime: {ElapsedTime}ms  Identifier: {DeploymentIdentifier}");
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
