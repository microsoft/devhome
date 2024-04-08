// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DevHome.SetupFlow.Models.Environments;

/// <summary>
/// Message for sending the data payload for the <see cref="Microsoft.Windows.DevHome.SDK.IExtensionAdaptiveCardSession2">
/// object's session back to a subscriber when the session ends.
/// </summary>
public class CreationAdaptiveCardSessionEndedMessage : ValueChangedMessage<CreationAdaptiveCardSessionEndedData>
{
    public CreationAdaptiveCardSessionEndedMessage(CreationAdaptiveCardSessionEndedData value)
        : base(value)
    {
    }
}
