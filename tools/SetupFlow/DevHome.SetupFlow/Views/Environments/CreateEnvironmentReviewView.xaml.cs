// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Messaging;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.ViewModels.Environments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views.Environments;

public sealed partial class CreateEnvironmentReviewView : UserControl, IRecipient<CreationOptionsReviewPageDataRequest>
{
    public CreateEnvironmentReviewViewModel ViewModel => (CreateEnvironmentReviewViewModel)this.DataContext;

    public CreateEnvironmentReviewView()
    {
        this.InitializeComponent();
        WeakReferenceMessenger.Default.Register<CreationOptionsReviewPageDataRequest>(this);
    }

    private void ViewUnloaded(object sender, RoutedEventArgs e)
    {
        AdaptiveCardGrid.Children.Clear();
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    /// <summary>
    /// Recieves the adaptive card from the view model, when the view model finishes loading it.
    /// </summary>
    public void Receive(CreationOptionsReviewPageDataRequest message)
    {
        // Recreate the adaptive card so we don't crash if the card already has a parent.
        var renderedAdaptiveCard = message.AdaptiveCardRenderer.RenderAdaptiveCard(message.AdaptiveCard);

        var frameworkElement = renderedAdaptiveCard?.FrameworkElement;
        if (frameworkElement == null)
        {
            return;
        }

        AdaptiveCardGrid.Children.Clear();
        AdaptiveCardGrid.Children.Add(frameworkElement);
    }
}
