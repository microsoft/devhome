// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public sealed partial class SummaryView : UserControl, IRecipient<NewAdaptiveCardAvailableMessage>
{
    public SummaryView()
    {
        this.InitializeComponent();
        WeakReferenceMessenger.Default.Register<NewAdaptiveCardAvailableMessage>(this);
    }

    public SummaryViewModel ViewModel => (SummaryViewModel)this.DataContext;

    private void ViewAllButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // When installation notes' 'view all' hyperlink is clicked, open a new window with the full text
        if (sender is HyperlinkButton hyperlinkButton && hyperlinkButton.Tag is PackageViewModel package)
        {
            var window = new InstallationNotesWindow(package.PackageTitle, package.InstallationNotes);
            window.Activate();
            window.CenterOnWindow();
        }
    }

    private void InstallationNotes_IsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
    {
        // Show 'view all' hyperlink if installation notes text is trimmed, otherwise hide it.
        if (sender?.Tag is HyperlinkButton viewAllButton)
        {
            viewAllButton.Visibility = sender.IsTextTrimmed ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void ViewUnloaded(object sender, RoutedEventArgs e)
    {
        AdaptiveCardGrid.Children.Clear();
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    /// <summary>
    /// Receive the adaptive card from the view model, when the view model finishes loading it.
    /// Note: There are times when the view is loaded after the view model has finished loading the adaptive card.
    /// In these cases it would have "missed" the push message. This is where the ViewLoaded method comes in.
    /// </summary>
    public void Receive(NewAdaptiveCardAvailableMessage message)
    {
        // Only process the message if the view model is the SummaryViewModel
        if (message.Value.CurrentSetupFlowViewModel is SummaryViewModel)
        {
            AddAdaptiveCardToUI(message.Value.RenderedAdaptiveCard);
        }
    }

    /// <summary>
    /// Request the adaptive cad from the EnvironmentCreationOptionsViewModel object when we're in the environment
    /// creation flow.
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
