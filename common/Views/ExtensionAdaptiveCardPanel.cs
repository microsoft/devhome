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

// XAML element to contain a single instance of extension UI.
// Use this element where extension UI is expected to pop up.
// https://github.com/microsoft/devhome/issues/610
public class ExtensionAdaptiveCardPanel : StackPanel
{
    public event EventHandler<FrameworkElement>? UiUpdate;

    // The rendered adaptive card is stored here so that it does not go out of scope.
    // There should only be one rendered adaptive card at a time for one ExtensionAdaptiveCardPanel.
    public RenderedAdaptiveCard? RenderedAdaptiveCard
    {
        get; set;
    }

    public void Bind(IExtensionAdaptiveCardSession extensionAdaptiveCardSession, AdaptiveCardRenderer? customRenderer)
    {
        var adaptiveCardRenderer = customRenderer ?? new AdaptiveCardRenderer();

        if (Children.Count != 0)
        {
            throw new ArgumentException("The ExtensionUI element must be bound to an empty container.");
        }

        var uiDispatcher = DispatcherQueue.GetForCurrentThread();
        var extensionUI = new ExtensionAdaptiveCard();

        extensionUI.UiUpdate += (object? sender, AdaptiveCard adaptiveCard) =>
        {
            uiDispatcher.TryEnqueue(() =>
            {
                var renderedAdaptiveCard = adaptiveCardRenderer.RenderAdaptiveCard(adaptiveCard);
                renderedAdaptiveCard.Action += async (RenderedAdaptiveCard? sender, AdaptiveActionEventArgs args) =>
                {
                    await extensionAdaptiveCardSession.OnAction(args.Action.ToJson().Stringify(), args.Inputs.AsJson().Stringify());
                };

                Children.Clear();
                Children.Add(renderedAdaptiveCard.FrameworkElement);

                if (this.UiUpdate != null)
                {
                    this.UiUpdate.Invoke(this, renderedAdaptiveCard.FrameworkElement);
                }

                RenderedAdaptiveCard = renderedAdaptiveCard;
            });
        };

        extensionAdaptiveCardSession.Initialize(extensionUI);
    }
}
