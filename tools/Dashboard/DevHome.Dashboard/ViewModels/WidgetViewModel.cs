// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using AdaptiveCards.Templating;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Dashboard.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;
using Windows.System;

namespace DevHome.Dashboard.ViewModels;

public partial class WidgetViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
    private readonly AdaptiveCardRenderer _renderer;

    [ObservableProperty]
    private Widget _widget;

    [ObservableProperty]
    private WidgetDefinition _widgetDefinition;

    [ObservableProperty]
    private WidgetSize _widgetSize;

    [ObservableProperty]
    private string _widgetDisplayTitle;

    [ObservableProperty]
    private FrameworkElement _widgetFrameworkElement;

    [ObservableProperty]
    private Microsoft.UI.Xaml.Media.Brush _widgetBackground;

    [ObservableProperty]
    private bool _isInEditMode;

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
            RenderWidgetFrameworkElement();
        }
    }

    partial void OnWidgetDefinitionChanged(WidgetDefinition value)
    {
        if (WidgetDefinition != null)
        {
            WidgetDisplayTitle = WidgetDefinition.DisplayTitle;
        }
    }

    partial void OnWidgetFrameworkElementChanged(FrameworkElement value)
    {
        if (WidgetFrameworkElement != null && WidgetFrameworkElement is Grid grid)
        {
            WidgetBackground = grid.Background;
        }
    }

    public WidgetViewModel(
        Widget widget,
        WidgetSize widgetSize,
        WidgetDefinition widgetDefintion,
        AdaptiveCardRenderer renderer,
        Microsoft.UI.Dispatching.DispatcherQueue dispatcher)
    {
        _renderer = renderer;
        _dispatcher = dispatcher;

        Widget = widget;
        WidgetSize = widgetSize;
        WidgetDefinition = widgetDefintion;
    }

    private async void RenderWidgetFrameworkElement()
    {
        Log.Logger()?.ReportDebug("WidgetViewModel", "RenderWidgetFrameworkElement");

        var cardTemplate = await Widget.GetCardTemplateAsync();
        var cardData = await Widget.GetCardDataAsync();

        if (string.IsNullOrEmpty(cardTemplate))
        {
            // TODO CreateWidgetAsync doesn't always seem to be "done", and returns blank templates and data.
            // Put in small wait to avoid this.
            Log.Logger()?.ReportWarn("WidgetViewModel", "Widget.GetCardTemplateAsync returned empty, try wait");
            await System.Threading.Tasks.Task.Delay(100);
            cardTemplate = await Widget.GetCardTemplateAsync();
            cardData = await Widget.GetCardDataAsync();
        }

        if (string.IsNullOrEmpty(cardData))
        {
            Log.Logger()?.ReportWarn("WidgetViewModel", "Widget.GetCardDataAsync returned empty, didn't render card.");
            ShowErrorCard("This widget could not be rendered because it has no data.");
            return;
        }

        Log.Logger()?.ReportDebug("WidgetViewModel", $"cardTemplate = {cardTemplate}");
        Log.Logger()?.ReportDebug("WidgetViewModel", $"cardData = {cardData}");

        // Use the data to fill in the template.
        AdaptiveCardParseResult card;
        try
        {
            var template = new AdaptiveCardTemplate(cardTemplate);
            var json = template.Expand(cardData);
            card = AdaptiveCard.FromJsonString(json);
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportWarn("WidgetViewModel", "There was an error expanding the Widget template with data.", ex);
            ShowErrorCard("This widget could not be rendered because the template could not be expanded with data.");
            return;
        }

        // Render card on the UI thread.
        _dispatcher.TryEnqueue(() =>
        {
            try
            {
                var renderedCard = _renderer.RenderAdaptiveCard(card.AdaptiveCard);
                if (renderedCard != null && renderedCard.FrameworkElement != null)
                {
                    renderedCard.Action += HandleAdaptiveAction;
                    WidgetFrameworkElement = renderedCard.FrameworkElement;
                }
            }
            catch (Exception e)
            {
                Log.Logger()?.ReportError("WidgetViewModel", "Error rendering widget card.", e);
                WidgetFrameworkElement = GetErrorCard("This widget could not be rendered.");
            }
        });
    }

    private void ShowErrorCard(string message)
    {
        _dispatcher.TryEnqueue(() =>
        {
            // TODO: Create nice fallback element with localized text.
            WidgetFrameworkElement = GetErrorCard(message);
        });
    }

    private FrameworkElement GetErrorCard(string message)
    {
        return new TextBlock
        {
            Text = message,
        };
    }

    private async void HandleAdaptiveAction(RenderedAdaptiveCard sender, AdaptiveActionEventArgs args)
    {
        Log.Logger()?.ReportInfo("WidgetViewModel", $"HandleInvokedAction {nameof(args.Action)} for widget {Widget.Id}");
        if (args.Action is AdaptiveOpenUrlAction openUrlAction)
        {
            Log.Logger()?.ReportInfo("WidgetViewModel", $"Url = {openUrlAction.Url}");
            await Launcher.LaunchUriAsync(openUrlAction.Url);
        }
        else if (args.Action is AdaptiveExecuteAction executeAction)
        {
            var dataToSend = string.Empty;
            var dataType = executeAction.DataJson.ValueType;
            if (dataType != Windows.Data.Json.JsonValueType.Null)
            {
                dataToSend = executeAction.DataJson.Stringify();
            }
            else
            {
                var inputType = args.Inputs.AsJson().ValueType;
                if (inputType != Windows.Data.Json.JsonValueType.Null)
                {
                    dataToSend = args.Inputs.AsJson().Stringify();
                }
            }

            Log.Logger()?.ReportInfo("WidgetViewModel", $"Verb = {executeAction.Verb}, Data = {dataToSend}");
            await Widget.NotifyActionInvokedAsync(executeAction.Verb, dataToSend);
        }

        // TODO: Handle other ActionTypes
    }

    private void HandleWidgetUpdated(Widget sender, WidgetUpdatedEventArgs args)
    {
        Log.Logger()?.ReportInfo("WidgetViewModel", $"HandleWidgetUpdated for widget {sender.Id}");
        RenderWidgetFrameworkElement();
    }
}
