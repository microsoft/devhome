// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DevHome.Environments.Models;

public class ComputeSystemOperationStartedMessage : ValueChangedMessage<ComputeSystemOperationStartedData>
{
    public ComputeSystemOperationStartedMessage(ComputeSystemOperationStartedData value)
        : base(value)
    {
    }
}
