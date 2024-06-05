// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Helpers;
using DevHome.Common.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.Common.Views;

// XAML element to contain a single instance of extension UI.
// Use this element where extension UI is expected to pop up.
public class ExtensionAdaptiveCardPanel : StackPanel
{
    public event EventHandler<FrameworkElement>? UiUpdate;

    private RenderedAdaptiveCard? _renderedAdaptiveCard;

    private string _oldTemplate = string.Empty;
    private string _currentTemplate = string.Empty;
    private List<int> focusedElementPath = new();

    public void Bind(IExtensionAdaptiveCardSession extensionAdaptiveCardSession, AdaptiveCardRenderer? customRenderer)
    {
        var adaptiveCardRenderer = customRenderer ?? new AdaptiveCardRenderer();

        if (Children.Count != 0)
        {
            throw new ArgumentException("The ExtensionUI element must be bound to an empty container.");
        }

        var uiDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        var extensionUI = new ExtensionAdaptiveCard();

        extensionUI.UiUpdate += (object? sender, AdaptiveCard adaptiveCard) =>
        {
            uiDispatcher.TryEnqueue(() =>
            {
                focusedElementPath = GetPathToFocusedElement(_renderedAdaptiveCard);

                _currentTemplate = _renderedAdaptiveCard?.OriginatingCard.ToJson().ToString() ?? string.Empty;

                _renderedAdaptiveCard = adaptiveCardRenderer.RenderAdaptiveCard(adaptiveCard);
                _renderedAdaptiveCard.Action += async (RenderedAdaptiveCard? sender, AdaptiveActionEventArgs args) =>
                {
                    Log.Information($"RenderedAdaptiveCard.Action(): Called for {args.Action.Id}");
                    await extensionAdaptiveCardSession.OnAction(args.Action.ToJson().Stringify(), args.Inputs.AsJson().Stringify());
                };

                Children.Clear();
                Children.Add(_renderedAdaptiveCard.FrameworkElement);

                UiUpdate?.Invoke(this, _renderedAdaptiveCard.FrameworkElement);

                // Ensure the Widget's Layout is updated.
                _renderedAdaptiveCard.FrameworkElement.UpdateLayout();

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
                        AttemptToKeepFocus(focusedElementPath, uiDispatcher);
                    }
                    else
                    {
                        uiDispatcher.TryEnqueue(DispatcherQueuePriority.High, () =>
                        {
                            _renderedAdaptiveCard.FrameworkElement.Focus(FocusState.Programmatic);
                        });
                    }
                }

                _oldTemplate = _currentTemplate;

                Log.Information($"ExtensionAdaptiveCard.UiUpdate(): Event handler for UiUpdate finished successfully");
            });
        };

        extensionAdaptiveCardSession.Initialize(extensionUI);
        Log.Information($"ExtensionAdaptiveCardPanel.Bind(): Binding to AdaptiveCard session finished successfully");
    }

    private List<int> GetPathToFocusedElement(RenderedAdaptiveCard? rendered)
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
    private void GetPathOnWidgetTree(FrameworkElement? currentElement, FrameworkElement target, ref List<int> path)
    {
        if (currentElement == null)
        {
            return;
        }

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

    private void AttemptToKeepFocus(List<int> path, DispatcherQueue dispatcher)
    {
        if (_renderedAdaptiveCard == null)
        {
            return;
        }

        var element = _renderedAdaptiveCard.FrameworkElement;

        // Try to descend the tree until we get to the same position as the control previously focused.
        foreach (var i in path)
        {
            // If for some reason there is not a way to reach a similar control in the same position
            // because of changes on the size of elements in a list for example, we set the focus
            // back to the widget as a whole.
            if (i >= VisualTreeHelper.GetChildrenCount(element))
            {
                dispatcher.TryEnqueue(DispatcherQueuePriority.High, () =>
                {
                    _renderedAdaptiveCard.FrameworkElement.Focus(FocusState.Programmatic);
                });
                return;
            }

            element = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
        }

        // Set the focus to the object after we reach it.
        dispatcher.TryEnqueue(DispatcherQueuePriority.High, () =>
        {
            element?.Focus(FocusState.Programmatic);
        });
    }
}
