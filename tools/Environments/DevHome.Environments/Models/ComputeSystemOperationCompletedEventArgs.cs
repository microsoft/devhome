// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.TelemetryEvents.SetupFlow.Environments;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Environments.Models;

public class ComputeSystemOperationCompletedEventArgs : EventArgs
{
    public ComputeSystemOperations ComputeSystemOperation { get; private set; }

    public ComputeSystemOperationResult OperationResult { get; private set; }

    public Guid ActivityId { get; private set; }

    public ComputeSystemOperationCompletedEventArgs(ComputeSystemOperations computeSystemOperation, ComputeSystemOperationResult operationResult, Guid activityId)
    {
        ComputeSystemOperation = computeSystemOperation;
        OperationResult = operationResult;
        ActivityId = activityId;
    }
}
