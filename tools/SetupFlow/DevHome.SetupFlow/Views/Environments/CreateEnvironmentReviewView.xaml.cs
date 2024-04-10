// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.ViewModels.Environments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace DevHome.SetupFlow.Views.Environments;

public sealed partial class CreateEnvironmentReviewView : UserControl, IRecipient<NewAdaptiveCardAvailableMessage>
{
    // Logging to capture any adaptive card rendering exceptions so the app doesn't crash
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(CreateEnvironmentReviewView));

    public CreateEnvironmentReviewViewModel ViewModel => (CreateEnvironmentReviewViewModel)this.DataContext;

    public CreateEnvironmentReviewView()
    {
        this.InitializeComponent();
        WeakReferenceMessenger.Default.Register<NewAdaptiveCardAvailableMessage>(this);
    }

    private void ViewUnloaded(object sender, RoutedEventArgs e)
    {
        AdaptiveCardGrid.Children.Clear();
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    /// <summary>
    /// Recieves the adaptive card from the view model, when the view model finishes loading it.
    /// </summary>
    public void Receive(NewAdaptiveCardAvailableMessage message)
    {
        // Only process the message if the view model is the ReviewViewModel
        if (message.Value.CurrentSetupFlowViewModel is ReviewViewModel)
        {
            AddAdaptiveCardToUI(message.Value.RenderedAdaptiveCard);
        }
    }

    /// <summary>
    /// Request the adaptive cad from the view model
    /// </summary>
    private void ViewLoaded(object sender, RoutedEventArgs e)
    {
        var message = WeakReferenceMessenger.Default.Send<CreationOptionsReviewPageDataRequestMessage>();
        if (!message.HasReceivedResponse)
        {
            return;
        }

        AddAdaptiveCardToUI(message.Response);
    }

    private void AddAdaptiveCardToUI(RenderedAdaptiveCard renderedAdaptiveCard)
    {
        try
        {
            var frameworkElement = renderedAdaptiveCard?.FrameworkElement;
            if (frameworkElement == null)
            {
                return;
            }

            AdaptiveCardGrid.Children.Clear();
            AdaptiveCardGrid.Children.Add(frameworkElement);
        }
        catch (Exception ex)
        {
            // Log the exception
            _log.Error(ex, "Error adding adaptive card UI in CreateEnvironmentReviewView");
        }
    }
}
