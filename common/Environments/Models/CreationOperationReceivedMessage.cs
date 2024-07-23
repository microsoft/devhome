// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Messaging.Messages;
using DevHome.Common.Environments.Models;

namespace DevHome.SetupFlow.Models.Environments;

/// <summary>
/// Message used to notify that a creation operation has been received.
/// </summary>
public class CreationOperationReceivedMessage : ValueChangedMessage<CreateComputeSystemOperation>
{
    public CreationOperationReceivedMessage(CreateComputeSystemOperation value)
        : base(value)
    {
    }
}
