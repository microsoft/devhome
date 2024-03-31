// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DevHome.SetupFlow.Models.Environments;

public class CreationAdaptiveCardSessionEndedMessage : ValueChangedMessage<CreationAdaptiveCardSessionEndedData>
{
    public CreationAdaptiveCardSessionEndedMessage(CreationAdaptiveCardSessionEndedData value)
        : base(value)
    {
    }
}
