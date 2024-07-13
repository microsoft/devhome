// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Models;
using DevHome.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Views;

// XAML element to contain a single instance of extension UI.
// Use this element where extension UI is expected to pop up.
public class ExtensionAdaptiveCardPanel : StackPanel
{
    public event EventHandler<FrameworkElement>? UiUpdate;

    private RenderedAdaptiveCard? _renderedAdaptiveCard;

    public AdaptiveCard? CurrentAdaptiveCard { get; private set; }

    public void Bind(
        IExtensionAdaptiveCardSession extensionAdaptiveCardSession,
        AdaptiveCardRenderer? customRenderer = null,
        AdaptiveElementParserRegistration? elementParserRegistration = null,
        AdaptiveActionParserRegistration? actionParserRegistration = null)
    {
        var adaptiveCardRenderer = customRenderer ?? new AdaptiveCardRenderer();

        if (Children.Count != 0)
        {
            throw new ArgumentException("The ExtensionUI element must be bound to an empty container.");
        }

        var uiDispatcher = DispatcherQueue.GetForCurrentThread();
        ExtensionAdaptiveCard extensionUI;

        // Setup adaptive card to use custom element parsers when requested
        if (elementParserRegistration != null && actionParserRegistration != null)
        {
            extensionUI = new ExtensionAdaptiveCard(elementParserRegistration, actionParserRegistration);
        }
        else
        {
            // Default parsers used
            extensionUI = new ExtensionAdaptiveCard();
        }

        extensionUI.UiUpdate += (object? sender, AdaptiveCard adaptiveCard) =>
        {
            uiDispatcher.TryEnqueue(() =>
            {
                _renderedAdaptiveCard = adaptiveCardRenderer.RenderAdaptiveCard(adaptiveCard);
                CurrentAdaptiveCard = adaptiveCard;
                _renderedAdaptiveCard.Action += async (RenderedAdaptiveCard? sender, AdaptiveActionEventArgs args) =>
                {
                    GlobalLog.Logger?.ReportInfo($"RenderedAdaptiveCard.Action(): Called for {args.Action.Id}");
                    await extensionAdaptiveCardSession.OnAction(args.Action.ToJson().Stringify(), args.Inputs.AsJson().Stringify());
                };

                Children.Clear();
                Children.Add(_renderedAdaptiveCard.FrameworkElement);

                UiUpdate?.Invoke(this, _renderedAdaptiveCard.FrameworkElement);
                GlobalLog.Logger?.ReportInfo($"ExtensionAdaptiveCard.UiUpdate(): Event handler for UiUpdate finished successfully");
            });
        };

        extensionAdaptiveCardSession.Initialize(extensionUI);
        GlobalLog.Logger?.ReportInfo($"ExtensionAdaptiveCardPanel.Bind(): Binding to AdaptiveCard session finished successfully");
    }
}
