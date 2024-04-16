// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Environments.Models;

public class ComputeSystemOperationCompletedData
{
    public ComputeSystemOperations ComputeSystemOperation { get; private set; }

    public ComputeSystemOperationResult OperationResult { get; private set; }

    public Guid ActivityId { get; private set; }

    public ComputeSystemOperationCompletedData(ComputeSystemOperations computeSystemOperation, ComputeSystemOperationResult operationResult, Guid activityId)
    {
        ComputeSystemOperation = computeSystemOperation;
        OperationResult = operationResult;
        ActivityId = activityId;
    }
}
