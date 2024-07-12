// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.DevHomeAdaptiveCards.CardModels;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Views.AdaptiveCardViews;

/// <summary>
/// Content dialog with non-interactive content within an adaptive card
/// </summary>
public sealed partial class ContentDialogWithNonInteractiveContent : ContentDialog
{
    private readonly IThemeSelectorService _themeSelector;

    private readonly DevHomeContentDialogContent _content;

    public ContentDialogWithNonInteractiveContent(DevHomeContentDialogContent content, IThemeSelectorService themeSelector)
    {
        this.InitializeComponent();

        // Since we use the renderer service to allow the card to receive theming updates, we need to ensure the UI thread is used.
        var dispatcherQueue = Application.Current.GetService<DispatcherQueue>();
        dispatcherQueue.TryEnqueue(async () =>
        {
            Title = content.Title;
            PrimaryButtonText = content.PrimaryButtonText;
            var rendererService = Application.Current.GetService<AdaptiveCardRenderingService>();
            var renderer = await rendererService.GetRendererAsync();
            renderer.HostConfig.ContainerStyles.Default.BackgroundColor = Microsoft.UI.Colors.Transparent;
            var card = renderer.RenderAdaptiveCardFromJsonString(content.ContentDialogInternalAdaptiveCardJson?.Stringify() ?? string.Empty);
            Content = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = card.FrameworkElement,
            };
            RequestedTheme = themeSelector.IsDarkTheme() ? ElementTheme.Dark : ElementTheme.Light;
            SecondaryButtonText = content.SecondaryButtonText;
            this.Focus(FocusState.Programmatic);
        });

        _themeSelector = themeSelector;
        _themeSelector.ThemeChanged += OnThemeChanged;
        _content = content;
    }

    private async void OnThemeChanged(object? sender, ElementTheme newRequestedTheme)
    {
        RequestedTheme = newRequestedTheme;
        var rendererService = Application.Current.GetService<AdaptiveCardRenderingService>();
        var renderer = await rendererService.GetRendererAsync();
        renderer.HostConfig.ContainerStyles.Default.BackgroundColor = Microsoft.UI.Colors.Transparent;
        var card = renderer.RenderAdaptiveCardFromJsonString(_content.ContentDialogInternalAdaptiveCardJson?.Stringify() ?? string.Empty);
        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = card.FrameworkElement,
        };
    }
}
