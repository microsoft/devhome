// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.SetupFlow.Environments;

public enum EnvironmentsTelemetryStatus
{
    Started,
    Succeeded,
    Failed,
}

[EventData]
public class EnvironmentCreationUserEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public string ProviderId { get; }

    public string Status { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentCreationUserEvent"/> class.
    /// </summary>
    /// <param name="providerId">The Id of the compute system provider that initiated the creation operation</param>
    /// <param name="status">The status of the creation operation</param>
    public EnvironmentCreationUserEvent(string providerId, EnvironmentsTelemetryStatus status)
    {
        ProviderId = providerId;
        Status = status.ToString();
    }

    // Inherited but unused.
    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
    }
}
