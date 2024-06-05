// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    private FrameworkElement _adaptiveCardFrameWorkElement;

    public CreateEnvironmentReviewView()
    {
        this.InitializeComponent();
        WeakReferenceMessenger.Default.Register<NewAdaptiveCardAvailableMessage>(this);
    }

    private void ViewUnloaded(object sender, RoutedEventArgs e)
    {
        // View unloaded so re-add elements back to original card now that we're done
        if (_adaptiveCardFrameWorkElement is Grid cardGrid)
        {
            AddUIElementsToGrid(AdaptiveCardGrid, cardGrid);
        }

        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    /// <summary>
    /// Receives the adaptive card from the view model, when the view model finishes loading it.
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
            _adaptiveCardFrameWorkElement = renderedAdaptiveCard?.FrameworkElement;

            // The grid may be in use by other views. Remove all the child elements
            // then add them to this views page to further prevent exceptions.
            if (_adaptiveCardFrameWorkElement is Grid cardGrid)
            {
                AddUIElementsToGrid(cardGrid, AdaptiveCardGrid);
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            _log.Error(ex, "Error adding adaptive card UI in CreateEnvironmentReviewView");
        }
    }

    private void AddUIElementsToGrid(Grid gridToRemoveItems, Grid gridToAddItems)
    {
        var listOfElements = new List<UIElement>();
        foreach (var item in gridToRemoveItems.Children)
        {
            listOfElements.Add(item);
        }

        if (listOfElements.Count > 0)
        {
            gridToRemoveItems.Children.Clear();
            gridToAddItems.Children.Clear();
            foreach (var item in listOfElements)
            {
                gridToAddItems.Children.Add(item);
            }
        }
    }
}
