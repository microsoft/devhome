// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DevHome.SetupFlow.Models.Environments;

/// <summary>
/// Message for requesting a rendered adaptive card that was created from a <see cref="DevHome.Common.Models.ExtensionAdaptiveCard."/>
/// object in one view model to a view.
/// </summary>
/// <remarks>
/// This is used when a view that displays an adaptive card needs to request the rendered adaptive card from the view model.
/// The view in this case would not want to using Binding to bind to the adaptive card, but instead request it and then manually
/// add it to its UI. This prevents xaml binding crashes with "Element is already the child of another element" exceptions.
/// </remarks>
public sealed class CreationOptionsViewPageRequestMessage : RequestMessage<RenderedAdaptiveCard>
{
}
