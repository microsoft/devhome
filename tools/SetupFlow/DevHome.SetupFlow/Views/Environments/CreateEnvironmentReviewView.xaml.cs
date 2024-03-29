// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using AdaptiveCards.Templating;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.Common.DevHomeAdaptiveCards.CardModels;
using DevHome.Common.Renderers;
using DevHome.Common.Views;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.ViewModels.Environments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIEx.Messaging;

namespace DevHome.SetupFlow.Views.Environments;

public sealed partial class CreateEnvironmentReviewView : UserControl
{
    public CreateEnvironmentReviewViewModel ViewModel => (CreateEnvironmentReviewViewModel)this.DataContext;

    public CreateEnvironmentReviewView()
    {
        this.InitializeComponent();
    }

    private void ViewLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null && ReviewTabAdaptiveCardUI.Content == null)
        {
            ViewModel.LoadAdaptiveCardPanel();
            var curCard = ViewModel.ExtensionAdaptiveCardPanel?.CurrentAdaptiveCard;
            if (curCard != null)
            {
                var renderer = new AdaptiveCardRenderer();
                renderer.HostConfig.ContainerStyles.Default.BackgroundColor = Microsoft.UI.Colors.Transparent;
                var card = renderer.RenderAdaptiveCardFromJson(curCard.ToJson());
                ReviewTabAdaptiveCardUI.Content = card.FrameworkElement;
            }
        }
    }

    private void ViewUnloaded(object sender, RoutedEventArgs e)
    {
        ReviewTabAdaptiveCardUI.Content = null;
    }
}
