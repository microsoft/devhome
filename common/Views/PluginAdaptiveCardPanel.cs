// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;
using Newtonsoft.Json;

namespace DevHome.Common.Views;

// XAML element to contain a single instance of plugin UI.
// Use this element where plugin UI is expected to pop up.
// TODO: Should ideally not allow external children to be added through the `Children` property.
// https://github.com/microsoft/devhome/issues/610
public class PluginAdaptiveCardPanel : StackPanel
{
    public event EventHandler<FrameworkElement>? UiUpdate;

    public void Bind(IExtensionAdaptiveCardController pluginAdaptiveCardController, AdaptiveCardRenderer? customRenderer)
    {
        var adaptiveCardRenderer = customRenderer ?? new AdaptiveCardRenderer();

        if (Children.Count != 0)
        {
            throw new ArgumentException("The PluginUI element must be bound to an empty container.");
        }

        var uiDispatcher = DispatcherQueue.GetForCurrentThread();
        var pluginUI = new PluginAdaptiveCard();

        pluginUI.UiUpdate += (object? sender, AdaptiveCard adaptiveCard) =>
        {
            uiDispatcher.TryEnqueue(() =>
            {
                var renderedAdaptiveCard = adaptiveCardRenderer.RenderAdaptiveCard(adaptiveCard);
                renderedAdaptiveCard.Action += (RenderedAdaptiveCard? sender, AdaptiveActionEventArgs args) =>
                {
                    pluginAdaptiveCardController.OnAction(JsonConvert.SerializeObject(args.Action), JsonConvert.SerializeObject(args.Inputs));
                };

                Children.Clear();
                Children.Add(renderedAdaptiveCard.FrameworkElement);

                if (this.UiUpdate != null)
                {
                    this.UiUpdate.Invoke(this, renderedAdaptiveCard.FrameworkElement);
                }
            });
        };

        pluginAdaptiveCardController.Initialize(pluginUI);
    }
}
