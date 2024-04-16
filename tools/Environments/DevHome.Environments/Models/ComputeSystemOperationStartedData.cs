// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.TelemetryEvents.SetupFlow.Environments;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Environments.Models;

public class ComputeSystemOperationStartedData
{
    public ComputeSystemOperations ComputeSystemOperation { get; private set; }

    public EnvironmentsTelemetryStatus TelemetryStatus => EnvironmentsTelemetryStatus.Started;

    public Guid ActivityId { get; private set; }

    public ComputeSystemOperationStartedData(ComputeSystemOperations computeSystemOperation, Guid activityId)
    {
        ComputeSystemOperation = computeSystemOperation;
        ActivityId = activityId;
    }
}
