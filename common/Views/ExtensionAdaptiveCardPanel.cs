// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Contracts.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.Common.Views;

// XAML element to contain a single instance of extension UI.
// Use this element where extension UI is expected to pop up.
public class ExtensionAdaptiveCardPanel : StackPanel
{
    public event EventHandler<FrameworkElement>? UiUpdate;

    private RenderedAdaptiveCard? _renderedAdaptiveCard;

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
                _renderedAdaptiveCard = adaptiveCardRenderer.RenderAdaptiveCard(adaptiveCard);
                _renderedAdaptiveCard.Action += async (RenderedAdaptiveCard? sender, AdaptiveActionEventArgs args) =>
                {
                    Log.Information($"RenderedAdaptiveCard.Action(): Called for {args.Action.Id}");
                    await extensionAdaptiveCardSession.OnAction(args.Action.ToJson().Stringify(), args.Inputs.AsJson().Stringify());
                };

                Children.Clear();
                Children.Add(_renderedAdaptiveCard.FrameworkElement);

                UiUpdate?.Invoke(this, _renderedAdaptiveCard.FrameworkElement);
                Log.Information($"ExtensionAdaptiveCard.UiUpdate(): Event handler for UiUpdate finished successfully");
            });
        };

        extensionAdaptiveCardSession.Initialize(extensionUI);
        Log.Information($"ExtensionAdaptiveCardPanel.Bind(): Binding to AdaptiveCard session finished successfully");
    }
}
