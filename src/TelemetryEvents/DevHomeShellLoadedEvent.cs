// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.Tracing;
using DevHome.Common.Helpers;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;
using Microsoft.Windows.AppLifecycle;
using Serilog;

namespace DevHome.TelemetryEvents;

[EventData]
public class DevHomeShellLoadedEvent : EventBase
{
    public Guid DeploymentIdentifier { get; }

    public ExtendedActivationKind ActivationKind { get; }

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public DevHomeShellLoadedEvent(ExtendedActivationKind activationKind)
    {
        ActivationKind = activationKind;
        DeploymentIdentifier = Deployment.Identifier;
        var log = Log.ForContext("SourceContext", nameof(DevHomeShellLoadedEvent));
        log.Debug($"DevHome Startup Event, Identifier: {DeploymentIdentifier}");
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
