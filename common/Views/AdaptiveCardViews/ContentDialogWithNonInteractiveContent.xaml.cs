// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.DevHomeAdaptiveCards.CardModels;
using DevHome.Common.Renderers;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Views.AdaptiveCardViews;

/// <summary>
/// Content dialog with non-interactive content within an adaptive card
/// </summary>
public sealed partial class ContentDialogWithNonInteractiveContent : ContentDialog
{
    public ContentDialogWithNonInteractiveContent(DevHomeContentDialogContent content)
    {
        this.InitializeComponent();

        Title = content.Title;
        PrimaryButtonText = content.PrimaryButtonText;
        var renderer = new AdaptiveCardRenderer();
        renderer.HostConfig.ContainerStyles.Default.BackgroundColor = Microsoft.UI.Colors.Transparent;
        var card = renderer.RenderAdaptiveCardFromJsonString(content.ContentDialogInternalAdaptiveCardJson?.Stringify() ?? string.Empty);
        Content = card.FrameworkElement;
        SecondaryButtonText = content.SecondaryButtonText;
    }
}
