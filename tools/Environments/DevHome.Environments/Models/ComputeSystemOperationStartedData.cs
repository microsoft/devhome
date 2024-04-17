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

    public string AdditionalContext { get; private set; } = string.Empty;

    public ComputeSystemOperationStartedData(ComputeSystemOperations computeSystemOperation, string additionalContext, Guid activityId)
    {
        ComputeSystemOperation = computeSystemOperation;
        AdditionalContext = additionalContext;
        ActivityId = activityId;
    }
}
