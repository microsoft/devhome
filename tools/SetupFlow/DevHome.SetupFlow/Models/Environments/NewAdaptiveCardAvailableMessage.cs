// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DevHome.SetupFlow.Models.Environments;

public class NewAdaptiveCardAvailableMessage : ValueChangedMessage<RenderedAdaptiveCardData>
{
    public NewAdaptiveCardAvailableMessage(RenderedAdaptiveCardData value)
        : base(value)
    {
    }
}
