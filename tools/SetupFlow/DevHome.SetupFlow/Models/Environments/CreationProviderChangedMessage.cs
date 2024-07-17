// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Messaging.Messages;
using DevHome.Common.Environments.Models;

namespace DevHome.SetupFlow.Models.Environments;

/// <summary>
/// Message for sending the <see cref="ComputeSystemProviderDetails"/> from one view model to
/// another view model when the provider changes.
/// </summary>
public class CreationProviderChangedMessage : ValueChangedMessage<ComputeSystemProviderDetails>
{
    public CreationProviderChangedMessage(ComputeSystemProviderDetails value)
        : base(value)
    {
    }
}
