// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Common.TelemetryEvents.SetupFlow.Environments;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.TelemetryEvents.Environments;

[EventData]
public class EnvironmentOperationUserEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public string ProviderId { get; }

    public string Status { get; }

    public string OperationName { get; }

    public string ActivityId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentOperationUserEvent"/> class.
    /// </summary>
    /// <param name="status">The status of the launch operation</param>
    /// <param name="computeSystemOperation">An enum representing the compute system operation that was invoked</param>
    /// <param name="providerId">The Id of the compute system provider that owns the compute system that is being launched</param>
    public EnvironmentOperationUserEvent(EnvironmentsTelemetryStatus status, ComputeSystemOperations computeSystemOperation, string providerId, Guid activityId)
    {
        Status = status.ToString();
        OperationName = computeSystemOperation.ToString();
        ProviderId = providerId;
        ActivityId = activityId.ToString();
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // The only sensitive string is the developerID.  GetHashedDeveloperId is used to hash the developerId.
    }
}
