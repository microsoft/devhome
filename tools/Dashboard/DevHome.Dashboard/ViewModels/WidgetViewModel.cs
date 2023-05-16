// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using AdaptiveCards.Templating;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Renderers;
using DevHome.Dashboard.Helpers;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;
using Windows.Data.Json;
using Windows.System;

namespace DevHome.Dashboard.ViewModels;

public partial class WidgetViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
    private readonly AdaptiveCardRenderer _renderer;
    private readonly WidgetHandler _widgetHandler;

    private RenderedAdaptiveCard _renderedCard;

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

    public bool IsInAddMode { get; set; }

    [ObservableProperty]
    private bool _isInEditMode;

    [ObservableProperty]
    private bool _configuring;

    partial void OnWidgetChanging(Widget value)
    {
        if (Widget != null)
        {
            Widget.WidgetUpdated -= _widgetHandler.HandleWidgetUpdated;
        }
    }

    partial void OnWidgetChanged(Widget value)
    {
        if (Widget != null)
        {
            Widget.WidgetUpdated += _widgetHandler.HandleWidgetUpdated;
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
        _widgetHandler = new WidgetHandler(this);

        Widget = widget;
        WidgetSize = widgetSize;
        WidgetDefinition = widgetDefintion;
    }

    public void Render()
    {
        RenderWidgetFrameworkElement();
    }

    private async void RenderWidgetFrameworkElement()
    {
        var cardTemplate = await Widget.GetCardTemplateAsync();
        var cardData = await Widget.GetCardDataAsync();

        if (string.IsNullOrEmpty(cardTemplate))
        {
            // TODO CreateWidgetAsync doesn't always seem to be "done", and returns blank templates and data.
            // Put in small wait to avoid this.
            // https://github.com/microsoft/devhome/issues/643
            Log.Logger()?.ReportWarn("WidgetViewModel", "Widget.GetCardTemplateAsync returned empty, try wait");
            await System.Threading.Tasks.Task.Delay(100);
            cardTemplate = await Widget.GetCardTemplateAsync();
            cardData = await Widget.GetCardDataAsync();
        }

        if (string.IsNullOrEmpty(cardData))
        {
            Log.Logger()?.ReportWarn("WidgetViewModel", "Widget.GetCardDataAsync returned empty, cannot render card.");
            ShowErrorCard("WidgetErrorCardDisplayText");
            return;
        }

        Log.Logger()?.ReportDebug("WidgetViewModel", $"cardTemplate = {cardTemplate}");
        Log.Logger()?.ReportDebug("WidgetViewModel", $"cardData = {cardData}");

        // If we're in the Add or Edit dialog, check the cardData to see if the card is in a configuration state
        // or if it is pinnable yet. If still configuring, the Pin button will be disabled.
        if (IsInAddMode || IsInEditMode)
        {
            GetConfiguring(cardData);
        }

        // Use the data to fill in the template.
        AdaptiveCardParseResult card;
        try
        {
            var template = new AdaptiveCardTemplate(cardTemplate);
            var json = template.Expand(cardData);

            // Use custom parser.
            var elementParser = new AdaptiveElementParserRegistration();
            elementParser.Set(LabelGroup.CustomTypeString, new LabelGroupParser());

            // Create adaptive card.
            card = AdaptiveCard.FromJsonString(json, elementParser, new AdaptiveActionParserRegistration());
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportWarn("WidgetViewModel", "There was an error expanding the Widget template with data: ", ex);
            ShowErrorCard("WidgetErrorCardDisplayText");
            return;
        }

        if (_renderedCard != null)
        {
            _renderedCard.Action -= _widgetHandler.HandleAdaptiveAction;
        }

        if (card == null || card.AdaptiveCard == null)
        {
            Log.Logger()?.ReportError("WidgetViewModel", "Error in AdaptiveCardParseResult");
            ShowErrorCard("WidgetErrorCardDisplayText");
            return;
        }

        // Render card on the UI thread.
        _dispatcher.TryEnqueue(() =>
        {
            try
            {
                _renderedCard = _renderer.RenderAdaptiveCard(card.AdaptiveCard);
                if (_renderedCard != null && _renderedCard.FrameworkElement != null)
                {
                    _renderedCard.Action += _widgetHandler.HandleAdaptiveAction;
                    WidgetFrameworkElement = _renderedCard.FrameworkElement;
                }
                else
                {
                    Log.Logger()?.ReportError("WidgetViewModel", "Error in RenderedAdaptiveCard");
                    WidgetFrameworkElement = GetErrorCard("WidgetErrorCardDisplayText");
                }
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError("WidgetViewModel", "Error rendering widget card: ", ex);
                WidgetFrameworkElement = GetErrorCard("WidgetErrorCardDisplayText");
            }
        });
    }

    // Check if the card data indicates a configuration state. Configuring is bound to the Pin button and will disable it if true.
    private void GetConfiguring(string cardData)
    {
        var jsonObj = JsonObject.Parse(cardData);
        if (jsonObj != null)
        {
            var isConfiguring = jsonObj.GetNamedBoolean("configuring", false);
            _dispatcher.TryEnqueue(() =>
            {
                Configuring = isConfiguring;
            });
        }
    }

    // Used to show a message instead of Adaptive Card content in a widget.
    public void ShowErrorCard(string error, string subError = null)
    {
        _dispatcher.TryEnqueue(() =>
        {
            WidgetFrameworkElement = GetErrorCard(error, subError);
        });
    }

    private FrameworkElement GetErrorCard(string error, string subError = null)
    {
        var resourceLoader = new Microsoft.Windows.ApplicationModel.Resources.ResourceLoader("DevHome.Dashboard.pri", "DevHome.Dashboard/Resources");

        var grid = new Grid
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            Padding = new Thickness(15, 0, 15, 0),
        };

        var sp = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        var errorText = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.WrapWholeWords,
            FontWeight = FontWeights.Bold,
            Text = resourceLoader.GetString(error),
        };
        sp.Children.Add(errorText);

        if (subError is not null)
        {
            var subErrorText = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = resourceLoader.GetString(subError),
                Margin = new Thickness(0, 12, 0, 0),
            };

            sp.Children.Add(subErrorText);
        }

        grid.Children.Add(sp);
        return grid;
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
        // https://github.com/microsoft/devhome/issues/644
    }

    private void HandleWidgetUpdated(Widget sender, WidgetUpdatedEventArgs args)
    {
        Log.Logger()?.ReportDebug("WidgetViewModel", $"HandleWidgetUpdated for widget {sender.Id}");
        RenderWidgetFrameworkElement();
    }

    private class WidgetHandler
    {
        private readonly WeakReference<WidgetViewModel> _wvm;

        public WidgetHandler(WidgetViewModel widgetViewModel)
        {
            _wvm = new WeakReference<WidgetViewModel>(widgetViewModel);
        }

        public void HandleWidgetUpdated(Widget sender, WidgetUpdatedEventArgs args)
        {
            Log.Logger()?.ReportInfo("WidgetViewModel", $"HandleWidgetUpdated for widget {sender.Id}");
            if (_wvm.TryGetTarget(out var widgetViewModel))
            {
                widgetViewModel.RenderWidgetFrameworkElement();
            }
        }

        public async void HandleAdaptiveAction(RenderedAdaptiveCard sender, AdaptiveActionEventArgs args)
        {
            if (_wvm.TryGetTarget(out var widgetViewModel))
            {
                Log.Logger()?.ReportInfo("WidgetViewModel", $"HandleInvokedAction {nameof(args.Action)} for widget {widgetViewModel.Widget.Id}");
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
                    await widgetViewModel.Widget.NotifyActionInvokedAsync(executeAction.Verb, dataToSend);
                }

                // TODO: Handle other ActionTypes
            }
        }
    }
}
