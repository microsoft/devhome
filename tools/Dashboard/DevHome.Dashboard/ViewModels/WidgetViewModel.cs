// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using AdaptiveCards.Templating;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Renderers;
using DevHome.Common.Services;
using DevHome.Dashboard.ComSafeWidgetObjects;
using DevHome.Dashboard.Services;
using DevHome.Dashboard.TelemetryEvents;
using DevHome.Telemetry;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;
using Windows.Data.Json;

namespace DevHome.Dashboard.ViewModels;

/// <summary>
/// Delegate factory for creating widget view models
/// </summary>
/// <param name="widget">Widget</param>
/// <param name="widgetSize">WidgetSize</param>
/// <param name="widgetDefinition">WidgetDefinition</param>
/// <returns>Widget view model</returns>
public delegate WidgetViewModel WidgetViewModelFactory(
    ComSafeWidget widget,
    WidgetSize widgetSize,
    ComSafeWidgetDefinition widgetDefinition);

public partial class WidgetViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetViewModel));

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly WidgetAdaptiveCardRenderingService _renderingService;

    private RenderedAdaptiveCard _renderedCard;

    private string _oldTemplate;
    private string _currentTemplate;
    private List<int> focusedElementPath;

    [ObservableProperty]
    private ComSafeWidget _widget;

    [ObservableProperty]
    private ComSafeWidgetDefinition _widgetDefinition;

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

    partial void OnWidgetChanging(ComSafeWidget value)
    {
        if (Widget != null)
        {
            Widget.WidgetUpdated -= HandleWidgetUpdated;
        }
    }

    partial void OnWidgetChanged(ComSafeWidget value)
    {
        if (Widget != null)
        {
            Widget.WidgetUpdated += HandleWidgetUpdated;
            ShowWidgetContentIfAvailable();
        }
    }

    partial void OnWidgetDefinitionChanged(ComSafeWidgetDefinition value)
    {
        if (WidgetDefinition != null)
        {
            WidgetDisplayTitle = WidgetDefinition.DisplayTitle;
            WidgetProviderDisplayTitle = WidgetDefinition.ProviderDefinitionDisplayName;
            IsCustomizable = WidgetDefinition.IsCustomizable;
        }
    }

    public WidgetViewModel(
        ComSafeWidget widget,
        WidgetSize widgetSize,
        ComSafeWidgetDefinition widgetDefinition,
        WidgetAdaptiveCardRenderingService adaptiveCardRenderingService,
        DispatcherQueue dispatcherQueue)
    {
        _renderingService = adaptiveCardRenderingService;
        _dispatcherQueue = dispatcherQueue;

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

            if (string.IsNullOrEmpty(cardData) || string.IsNullOrEmpty(cardTemplate))
            {
                _log.Warning("Widget.GetCardDataAsync returned empty, cannot render card.");
                ShowErrorCard("WidgetErrorCardDisplayText");
                return;
            }

            // Uncomment for extra debugging output
            // _log.Debug($"cardTemplate = {cardTemplate}");
            // _log.Debug($"cardData = {cardData}");

            // Use the data to fill in the template.
            AdaptiveCardParseResult card;
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
                card = AdaptiveCard.FromJsonString(json, elementParser, new AdaptiveActionParserRegistration());
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "There was an error expanding the Widget template with data: ");
                ShowErrorCard("WidgetErrorCardDisplayText");
                return;
            }

            if (_renderedCard != null)
            {
                _renderedCard.Action -= HandleAdaptiveAction;
            }

            if (card == null || card.AdaptiveCard == null)
            {
                _log.Error("Error in AdaptiveCardParseResult");
                ShowErrorCard("WidgetErrorCardDisplayText");
                return;
            }

            // Render card on the UI thread.
            _dispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    focusedElementPath = GetPathToFocusedElement(_renderedCard);

                    var renderer = await _renderingService.GetRendererAsync();
                    _renderedCard = renderer.RenderAdaptiveCard(card.AdaptiveCard);
                    if (_renderedCard != null && _renderedCard.FrameworkElement != null)
                    {
                        _currentTemplate = cardTemplate;
                        _renderedCard.Action += HandleAdaptiveAction;
                        WidgetFrameworkElement = _renderedCard.FrameworkElement;

                        // Ensure the Widget's Layout is updated.
                        WidgetFrameworkElement.UpdateLayout();

                        // If the path has elements, the focused control is inside this widget.
                        // Otherwise, it is outside, so there is nothing else to do here.
                        if (focusedElementPath.Count > 0)
                        {
                            // If the template didn't change, the data structure is the same, so we can
                            // try to keep focus on the element that is in the same position.
                            // But if the template changed, we just reset the focus to the widget itself as
                            // the structure of the widget changed too.
                            if (_oldTemplate == _currentTemplate)
                            {
                                AttemptToKeepFocus(focusedElementPath);
                            }
                            else
                            {
                                _dispatcher.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
                                {
                                    WidgetFrameworkElement.Focus(FocusState.Programmatic);
                                });
                            }
                        }

                        _oldTemplate = _currentTemplate;
                    }
                    else
                    {
                        _log.Error("Error in RenderedAdaptiveCard");
                        WidgetFrameworkElement = GetErrorCard("WidgetErrorCardDisplayText");
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error rendering widget card: ");
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
                _log.Debug("Widget content not available yet.");
                return false;
            }

            _log.Debug("Widget content available.");
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
        _log.Debug("Show loading card.");
        _dispatcherQueue.TryEnqueue(() =>
        {
            WidgetFrameworkElement = new ProgressRing();
        });
    }

    // Used to show a message instead of Adaptive Card content in a widget.
    public void ShowErrorCard(string error, string subError = null)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            WidgetFrameworkElement = GetErrorCard(error, subError);
        });
    }

    private Grid GetErrorCard(string error, string subError = null)
    {
        var stringResource = new StringResource("DevHome.Dashboard.pri", "DevHome.Dashboard/Resources");

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
            Text = stringResource.GetLocalized(error),
        };
        sp.Children.Add(errorText);

        if (subError is not null)
        {
            var subErrorText = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = stringResource.GetLocalized(subError),
                Margin = new Thickness(0, 12, 0, 0),
            };

            sp.Children.Add(subErrorText);
        }

        grid.Children.Add(sp);
        return grid;
    }

    private async void HandleAdaptiveAction(RenderedAdaptiveCard sender, AdaptiveActionEventArgs args)
    {
        _log.Information($"HandleInvokedAction {args.Action.ActionTypeString} for widget {Widget.Id}");
        if (args.Action is AdaptiveOpenUrlAction openUrlAction)
        {
            _log.Information($"Url = {openUrlAction.Url}");
            await Windows.System.Launcher.LaunchUriAsync(openUrlAction.Url);
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

            _log.Information($"Verb = {executeAction.Verb}, Data = {dataToSend}");
            await Widget.NotifyActionInvokedAsync(executeAction.Verb, dataToSend);
        }

        TelemetryFactory.Get<ITelemetry>().Log(
            "Dashboard_ReportWidgetInteraction",
            LogLevel.Critical,
            new ReportWidgetInteractionEvent(WidgetDefinition.ProviderDefinitionId, WidgetDefinition.Id, args.Action.ActionTypeString));

        // TODO: Handle other ActionTypes
        // https://github.com/microsoft/devhome/issues/644
    }

    private async void HandleWidgetUpdated(ComSafeWidget sender, WidgetUpdatedEventArgs args)
    {
        _log.Debug($"HandleWidgetUpdated for widget {sender.Id}");
        await RenderWidgetFrameworkElementAsync();
    }

    public void UnsubscribeFromWidgetUpdates()
    {
        Widget.WidgetUpdated -= HandleWidgetUpdated;
    }

    private List<int> GetPathToFocusedElement(RenderedAdaptiveCard rendered)
    {
        var pathOnTree = new List<int>();

        // Empty path returned if there is no rendered card before
        if (rendered == null)
        {
            return pathOnTree;
        }

        try
        {
            // We get the current focused element. If it is not inside the widget, the path to be returned
            // will have no elements at all as we will not reach it in our search
            var focused = FocusManager.GetFocusedElement(rendered.FrameworkElement.XamlRoot) as FrameworkElement;

            if (focused != null)
            {
                GetPathOnWidgetTree(rendered.FrameworkElement, focused, ref pathOnTree);
            }

            // We build the path recursively, so it is reversed. We reverse it to get the path from root to leaf.
            pathOnTree.Reverse();
        }
        catch (Exception e)
        {
            Log.Logger()?.ReportError("WidgetViewModel", e.Message);
        }

        return pathOnTree;
    }

    // This method is a DFS to search the focused element inside the widget descendants
    private void GetPathOnWidgetTree(FrameworkElement currentElement, FrameworkElement target, ref List<int> path)
    {
        var num_children = VisualTreeHelper.GetChildrenCount(currentElement);
        for (var i = 0; i < num_children; ++i)
        {
            var child = VisualTreeHelper.GetChild(currentElement, i) as FrameworkElement;

            // If we find the focused element, we add its index on the final path we passed by reference.
            // This is expected to be the first item added.
            if (child == target)
            {
                path.Add(i);
                return;
            }

            GetPathOnWidgetTree(child, target, ref path);

            // If after we call the recusrion to a child, the path is not empty,
            // we fond the target on this subtree. We stop the search and add the child's
            // index to the answer.
            if (path.Count > 0)
            {
                path.Add(i);
                return;
            }
        }
    }

    private void AttemptToKeepFocus(List<int> path)
    {
        var element = WidgetFrameworkElement;

        // Try to descend the tree until we get to the same position as the control previously focused.
        foreach (var i in path)
        {
            // If for some reason there is not a way to reach a similar control in the same position
            // because of changes on the size of elements in a list for example, we set the focus
            // back to the widget as a whole.
            if (i >= VisualTreeHelper.GetChildrenCount(element))
            {
                _dispatcher.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
                {
                    WidgetFrameworkElement.Focus(FocusState.Programmatic);
                });
                return;
            }

            element = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
        }

        // Set the focus to the object after we reach it.
        _dispatcher.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
        {
            element.Focus(FocusState.Programmatic);
        });
    }
}
