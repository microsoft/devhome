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

    public string AdditionalContext { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentOperationUserEvent"/> class.
    /// </summary>
    /// <param name="status">The status of the launch operation</param>
    /// <param name="computeSystemOperation">An enum representing the compute system operation that was invoked</param>
    /// <param name="providerId">The Id of the compute system provider that owns the compute system that is being launched</param>
    /// <param name="additionalContext">The context in which the operation is running as. E.g the Pin to start operation can be for Pinning or Unpinning</param>
    /// <param name="activityId">The activity Id associated with the compute system operation that was invoked by the user</param>
    public EnvironmentOperationUserEvent(EnvironmentsTelemetryStatus status, ComputeSystemOperations computeSystemOperation, string providerId, string additionalContext, Guid activityId)
    {
        Status = status.ToString();
        OperationName = computeSystemOperation.ToString();
        ProviderId = providerId;
        AdditionalContext = additionalContext;
        ActivityId = activityId.ToString();
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // The only sensitive string is the developerID.  GetHashedDeveloperId is used to hash the developerId.
    }
}
