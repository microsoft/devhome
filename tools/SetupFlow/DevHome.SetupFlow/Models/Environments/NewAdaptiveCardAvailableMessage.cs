// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DevHome.SetupFlow.Models.Environments;

/// <summary>
/// Message for sending a rendered adaptive card that was created from a <see cref="DevHome.Common.Models.ExtensionAdaptiveCard."/>
/// object in one view model to a view.
/// </summary>
/// <remarks>
/// Since multiple view models can listen for this message in the setup flow, this object is used to to send the rendered adaptive card
/// as well as the current view model that is being displayed in the setup flow. Listeners can use the current view model in use
/// by the <see cref="Services.SetupFlowOrchestrator"/> to determine if they should display the adaptive card or not.
/// </remarks>
public class NewAdaptiveCardAvailableMessage : ValueChangedMessage<RenderedAdaptiveCardData>
{
    public NewAdaptiveCardAvailableMessage(RenderedAdaptiveCardData value)
        : base(value)
    {
    }
}
