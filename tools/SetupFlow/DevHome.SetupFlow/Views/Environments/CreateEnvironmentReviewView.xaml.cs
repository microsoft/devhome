// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.ViewModels.Environments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views.Environments;

public sealed partial class CreateEnvironmentReviewView : UserControl, IRecipient<NewAdaptiveCardAvailableMessage>
{
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
        var frameworkElement = renderedAdaptiveCard?.FrameworkElement;
        if (frameworkElement == null)
        {
            return;
        }

        AdaptiveCardGrid.Children.Clear();
        AdaptiveCardGrid.Children.Add(frameworkElement);
    }
}
