// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.SetupFlow.Environments;

[EventData]
public class EnvironmentLaunchUserEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public string ProviderId { get; }

    public string Status { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentLaunchUserEvent"/> class.
    /// </summary>
    /// <param name="providerId">The Id of the compute system provider that owns the compute system that is being launched</param>
    /// <param name="status">The status of the launch operation</param>
    public EnvironmentLaunchUserEvent(string providerId, EnvironmentsTelemetryStatus status)
    {
        ProviderId = providerId;
        Status = status.ToString();
    }

    // Inherited but unused.
    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
    }
}
