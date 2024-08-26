// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.SetupFlow.Controls;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views.Summary;

public sealed partial class SummaryShowAppsAndRepos : UserControl, IRecipient<NewAdaptiveCardAvailableMessage>
{
    public SummaryShowAppsAndRepos()
    {
        this.InitializeComponent();
        WeakReferenceMessenger.Default.Register<NewAdaptiveCardAvailableMessage>(this);
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

    private void ViewUnloaded(object sender, RoutedEventArgs e)
    {
        AdaptiveCardGrid.Children.Clear();
        WeakReferenceMessenger.Default.UnregisterAll(this);
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

    private void PackagesGridView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not GridView gridView)
        {
            return;
        }

        // Set the tooltip for each item in the grid view
        for (var i = 0; i < gridView.Items.Count; i++)
        {
            if (gridView.ContainerFromIndex(i) is GridViewItem item && item.Content is PackageViewModel packageViewModel)
            {
                ToolTipService.SetToolTip(item, new PackageDetailsTooltip()
                {
                    Package = packageViewModel,
                });
            }
        }
    }
}
