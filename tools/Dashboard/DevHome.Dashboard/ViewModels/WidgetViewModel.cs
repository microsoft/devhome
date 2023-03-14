// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using AdaptiveCards.Templating;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.ViewModels;

public partial class WidgetViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
    private readonly AdaptiveCardRenderer _renderer;

    [ObservableProperty]
    private Widget _widget;

    partial void OnWidgetChanging(Widget value)
    {
        if (Widget != null)
        {
            Widget.WidgetUpdated -= HandleWidgetUpdated;
        }
    }

    partial void OnWidgetChanged(Widget value)
    {
        if (Widget != null)
        {
            Widget.WidgetUpdated += HandleWidgetUpdated;
            RenderWidgetUIElement();
        }
    }

    private FrameworkElement _widgetUIElement;

    public FrameworkElement WidgetUIElement
    {
        get => _widgetUIElement;
        set => SetProperty(ref _widgetUIElement, value);
    }

    public WidgetViewModel(
        Widget widget,
        AdaptiveCardRenderer renderer,
        Microsoft.UI.Dispatching.DispatcherQueue dispatcher)
    {
        _renderer = renderer;
        _dispatcher = dispatcher;

        Widget = widget;
    }

    private async void RenderWidgetUIElement()
    {
        var cardTemplate = await _widget.GetCardTemplateAsync();
        var cardData = await _widget.GetCardDataAsync();

        if (!cardData.Equals(string.Empty, StringComparison.Ordinal))
        {
            // Use the data to fill in the template
            var template = new AdaptiveCardTemplate(cardTemplate);
            var json = template.Expand(cardData);

            // Render card on the UI thread
            _dispatcher.TryEnqueue(() =>
            {
                var card = AdaptiveCard.FromJsonString(json);
                var renderedCard = _renderer.RenderAdaptiveCard(card.AdaptiveCard);
                if (renderedCard != null && renderedCard.FrameworkElement != null)
                {
                    renderedCard.Action += HandleInvokedAction;
                    WidgetUIElement = renderedCard.FrameworkElement as Grid;
                }
            });
        }
    }

    private void HandleInvokedAction(RenderedAdaptiveCard sender, AdaptiveActionEventArgs args)
    {
        var actionExecute = args.Action as AdaptiveExecuteAction;
        if (actionExecute != null)
        {
            var dataToSend = string.Empty;
            var data = actionExecute.DataJson.Stringify();
            if (data != "null")
            {
                dataToSend = data;
            }
            else
            {
                var inputs = args.Inputs.AsJson().Stringify();
                if (inputs != "null")
                {
                    dataToSend = inputs;
                }
            }

            _ = _widget.NotifyActionInvokedAsync(actionExecute.Verb, dataToSend);
        }
    }

    private void HandleWidgetUpdated(Widget sender, WidgetUpdatedEventArgs args)
    {
        RenderWidgetUIElement();
    }
}
