// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DevHome.Environments.Models;

public class ComputeSystemOperationCompletedMessage : ValueChangedMessage<ComputeSystemOperationCompletedData>
{
    public ComputeSystemOperationCompletedMessage(ComputeSystemOperationCompletedData value)
        : base(value)
    {
    }
}
