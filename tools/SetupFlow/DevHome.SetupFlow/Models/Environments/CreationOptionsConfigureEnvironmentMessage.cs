// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DevHome.SetupFlow.Models.Environments;

public class CreationOptionsConfigureEnvironmentMessage : ValueChangedMessage<RenderedAdaptiveCard>
{
    public CreationOptionsConfigureEnvironmentMessage(RenderedAdaptiveCard value)
        : base(value)
    {
    }
}
