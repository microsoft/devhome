// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.ViewModels.Environments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views.Environments;

public sealed partial class EnvironmentCreationOptionsView : UserControl, IRecipient<NewAdaptiveCardAvailableMessage>
{
    public EnvironmentCreationOptionsViewModel ViewModel => (EnvironmentCreationOptionsViewModel)this.DataContext;

    public EnvironmentCreationOptionsView()
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
    /// Request the adaptive cad from the view model
    /// </summary>
    private void ViewLoaded(object sender, RoutedEventArgs e)
    {
        var message = WeakReferenceMessenger.Default.Send<CreationOptionsViewPageRequestMessage>();
        if (!message.HasReceivedResponse)
        {
            return;
        }

        AddAdaptiveCardToUI(message.Response);
    }

    /// <summary>
    /// Receive the adaptive card from the view model, when the view model finishes loading it.
    /// Note: There are times when the view is loaded after the view model has finished loading the adaptive card.
    /// In these cases it would have "missed" the push message. This is where the ViewLoaded method comes in.
    /// </summary>
    public void Receive(NewAdaptiveCardAvailableMessage message)
    {
        // Only process the message if the view model is the EnvironmentCreationOptionsViewModel
        if (message.Value.CurrentSetupFlowViewModel is EnvironmentCreationOptionsViewModel)
        {
            AddAdaptiveCardToUI(message.Value.RenderedAdaptiveCard);
        }
    }

    private void AddAdaptiveCardToUI(RenderedAdaptiveCard adaptiveCardData)
    {
        if (adaptiveCardData?.FrameworkElement != null)
        {
            AdaptiveCardGrid.Children.Clear();
            AdaptiveCardGrid.Children.Add(adaptiveCardData.FrameworkElement);
        }
    }
}
