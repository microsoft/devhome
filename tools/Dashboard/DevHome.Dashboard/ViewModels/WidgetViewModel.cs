// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using AdaptiveCards.Templating;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Renderers;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.Services;
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
    private readonly IAdaptiveCardRenderingService _renderingService;

    private RenderedAdaptiveCard _renderedCard;

    [ObservableProperty]
    private Widget _widget;

    [ObservableProperty]
    private WidgetDefinition _widgetDefinition;

    [ObservableProperty]
    private WidgetSize _widgetSize;

    [ObservableProperty]
    private bool _isCustomizable;

    [ObservableProperty]
    private string _widgetDisplayTitle;

    [ObservableProperty]
    private string _widgetProviderDisplayTitle;

    [ObservableProperty]
    private FrameworkElement _widgetFrameworkElement;

    public bool IsInAddMode { get; set; }
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
            _everGotDataAndTemplate = false;
            Widget.WidgetUpdated += HandleWidgetUpdated;
            ShowWidgetContentIfAvailable();
        }
    }

    partial void OnWidgetDefinitionChanged(WidgetDefinition value)
    {
        if (WidgetDefinition != null)
        {
            WidgetDisplayTitle = WidgetDefinition.DisplayTitle;
            WidgetProviderDisplayTitle = WidgetDefinition.ProviderDefinition.DisplayName;
            IsCustomizable = WidgetDefinition.IsCustomizable;
        }
    }

    private bool _everGotDataAndTemplate;

    public WidgetViewModel(
        Widget widget,
        WidgetSize widgetSize,
        WidgetDefinition widgetDefinition,
        Microsoft.UI.Dispatching.DispatcherQueue dispatcher)
    {
        _renderingService = Application.Current.GetService<IAdaptiveCardRenderingService>();
        _dispatcher = dispatcher;

        Widget = widget;
        WidgetSize = widgetSize;
        WidgetDefinition = widgetDefinition;
    }

    public async Task RenderAsync()
    {
        await RenderWidgetFrameworkElementAsync();
    }

    private async Task RenderWidgetFrameworkElementAsync()
    {
        await Task.Run(async () =>
        {
            var cardTemplate = await Widget.GetCardTemplateAsync();
            var cardData = await Widget.GetCardDataAsync();

            Log.Logger()?.ReportDebug("WidgetViewModel", $"cardTemplate = {cardTemplate}");
            Log.Logger()?.ReportDebug("WidgetViewModel", $"cardData = {cardData}");

            if (string.IsNullOrEmpty(cardData) || string.IsNullOrEmpty(cardTemplate))
            {
                if (!_everGotDataAndTemplate)
                {
                    // If we've never rendered the card before, the provider might just need more time to start up.
                    Log.Logger()?.ReportWarn("WidgetViewModel", "Something returned empty, cannot render card yet.");
                    ShowLoadingCard();
                    return;
                }
                else
                {
                    // If we have rendered the card, something went wrong and we should show an error.
                    Log.Logger()?.ReportWarn("WidgetViewModel", "Something returned empty, cannot render card.");
                    ShowErrorCard("WidgetErrorCardDisplayText");
                    return;
                }
            }

            _everGotDataAndTemplate = true;

            // Use the data to fill in the template.
            AdaptiveCardParseResult cardResult;
            try
            {
                var template = new AdaptiveCardTemplate(cardTemplate);

                var hostData = new JsonObject
                {
                    // TODO Add support to host theme in hostData
                    { "widgetSize", JsonValue.CreateStringValue(WidgetSize.ToString().ToLowerInvariant()) }, // "small", "medium" or "large"
                }.ToString();

                var context = new EvaluationContext(cardData, hostData);
                var json = template.Expand(context);

                // Use custom parser.
                var elementParser = new AdaptiveElementParserRegistration();
                elementParser.Set(LabelGroup.CustomTypeString, new LabelGroupParser());

                // Create adaptive card.
                cardResult = AdaptiveCard.FromJsonString(json, elementParser, new AdaptiveActionParserRegistration());
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportWarn("WidgetViewModel", "There was an error expanding the Widget template with data: ", ex);
                ShowErrorCard("WidgetErrorCardDisplayText");
                return;
            }

            if (_renderedCard != null)
            {
                _renderedCard.Action -= HandleAdaptiveAction;
            }

            if (cardResult == null || cardResult.AdaptiveCard == null)
            {
                Log.Logger()?.ReportError("WidgetViewModel", "Error in AdaptiveCardParseResult");
                ShowErrorCard("WidgetErrorCardDisplayText", cardResult: cardResult);
                return;
            }

            // Render card on the UI thread.
            _dispatcher.TryEnqueue(async () =>
            {
                try
                {
                    var renderer = await _renderingService.GetRenderer();
                    _renderedCard = renderer.RenderAdaptiveCard(cardResult.AdaptiveCard);
                    if (_renderedCard != null && _renderedCard.FrameworkElement != null)
                    {
                        _renderedCard.Action += HandleAdaptiveAction;
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
        });
    }

    private async Task<bool> IsWidgetContentAvailable()
    {
        return await Task.Run(async () =>
        {
            var cardTemplate = await Widget.GetCardTemplateAsync();
            var cardData = await Widget.GetCardDataAsync();

            if (string.IsNullOrEmpty(cardTemplate) || string.IsNullOrEmpty(cardData))
            {
                Log.Logger()?.ReportDebug("WidgetViewModel", "Widget content not available yet.");
                return false;
            }

            Log.Logger()?.ReportDebug("WidgetViewModel", "Widget content available.");
            return true;
        });
    }

    // If widget content (fresh or cached) is available, show it.
    // Otherwise, show the loading card until the widget updates itself.
    private async void ShowWidgetContentIfAvailable()
    {
        if (await IsWidgetContentAvailable())
        {
            await RenderWidgetFrameworkElementAsync();
        }
        else
        {
            ShowLoadingCard();
        }
    }

    // Used to show a loading ring when we don't have widget content.
    public void ShowLoadingCard()
    {
        Log.Logger()?.ReportDebug("WidgetViewModel", "Show loading card.");
        _dispatcher.TryEnqueue(() =>
        {
            WidgetFrameworkElement = new ProgressRing();
        });
    }

    // Used to show a message instead of Adaptive Card content in a widget.
    public void ShowErrorCard(string error, string subError = null, AdaptiveCardParseResult cardResult = null)
    {
        _dispatcher.TryEnqueue(() =>
        {
            WidgetFrameworkElement = GetErrorCard(error, subError, cardResult);
        });
    }

    private Grid GetErrorCard(string error, string subError = null, AdaptiveCardParseResult cardResult = null)
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
#if DEBUG
        if (cardResult != null)
        {
            foreach (var adaptiveError in cardResult.Errors)
            {
                var adaptiveErrorBlock = new TextBlock
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Text = resourceLoader.GetString("Error: " + adaptiveError.StatusCode.ToString() + ": " + adaptiveError.Message),
                    Margin = new Thickness(0, 12, 0, 0),
                };
                sp.Children.Add(adaptiveErrorBlock);
            }

            foreach (var adaptiveWarning in cardResult.Warnings)
            {
                var adaptiveWarningBlock = new TextBlock
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Text = resourceLoader.GetString("Warning: " + adaptiveWarning.StatusCode + ": " + adaptiveWarning.Message),
                    Margin = new Thickness(0, 12, 0, 0),
                };
                sp.Children.Add(adaptiveWarningBlock);
            }
        }
#endif

        grid.Children.Add(sp);
        return grid;
    }

    private void ShowLoadingCard()
    {
        _dispatcher.TryEnqueue(() =>
        {
            WidgetFrameworkElement = GetLoadingCard();
        });
    }

    private FrameworkElement GetLoadingCard()
    {
        var grid = new Grid();
        var spinner = new ProgressRing();
        grid.Children.Add(spinner);
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

    private async void HandleWidgetUpdated(Widget sender, WidgetUpdatedEventArgs args)
    {
        Log.Logger()?.ReportDebug("WidgetViewModel", $"HandleWidgetUpdated for widget {sender.Id}");
        await RenderWidgetFrameworkElementAsync();
    }
}
