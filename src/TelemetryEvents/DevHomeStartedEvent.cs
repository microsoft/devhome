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
public class DevHomeStartedEvent : EventBase
{
    public Guid DeploymentIdentifier { get; }

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public DevHomeStartedEvent()
    {
        DeploymentIdentifier = Deployment.Identifier;
        var log = Log.ForContext("SourceContext", nameof(DevHomeStartedEvent));
        log.Debug($"DevHome Launched Event, Identifier: {DeploymentIdentifier}");
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
