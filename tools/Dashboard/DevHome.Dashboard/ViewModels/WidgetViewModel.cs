// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using AdaptiveCards.Templating;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Renderers;
using DevHome.Dashboard.Helpers;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;
using Windows.Data.Json;
using Windows.System;

namespace DevHome.Dashboard.ViewModels;

public partial class WidgetViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
    private readonly AdaptiveCardRenderer _renderer;

    private RenderedAdaptiveCard _renderedCard;

    private string _oldTemplate;
    private string _currentTemplate;
    private List<int> focusedElementPath;

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

    private void OnWidgetFrameworkElementLoaded(object sender, RoutedEventArgs e)
    {
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
                    _dispatcher.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        WidgetFrameworkElement.Focus(FocusState.Programmatic);
                    });
                }
            }

            _oldTemplate = _currentTemplate;
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
        WidgetDefinition widgetDefinition,
        AdaptiveCardRenderer renderer,
        Microsoft.UI.Dispatching.DispatcherQueue dispatcher)
    {
        _renderer = renderer;
        _dispatcher = dispatcher;

        Widget = widget;
        WidgetSize = widgetSize;
        WidgetDefinition = widgetDefinition;
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
        // or if it is able to be pinned yet. If still configuring, the Pin button will be disabled.
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
            _renderedCard.Action -= HandleAdaptiveAction;
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
                focusedElementPath = GetPathToFocusedElement(_renderedCard);

                _renderedCard = _renderer.RenderAdaptiveCard(card.AdaptiveCard);
                if (_renderedCard != null && _renderedCard.FrameworkElement != null)
                {
                    _currentTemplate = cardTemplate;
                    _renderedCard.Action += HandleAdaptiveAction;
                    WidgetFrameworkElement = _renderedCard.FrameworkElement;
                    WidgetFrameworkElement.Loaded += OnWidgetFrameworkElementLoaded;
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
                WidgetFrameworkElement.Focus(FocusState.Keyboard);
                return;
            }

            element = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
        }

        // Set the focus to the object after we reach it.
        _dispatcher.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            element.Focus(FocusState.Programmatic);
        });
    }
}
