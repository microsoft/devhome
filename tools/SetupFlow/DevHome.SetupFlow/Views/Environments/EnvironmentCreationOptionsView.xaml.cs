// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using AdaptiveCards.Templating;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.Common.DevHomeAdaptiveCards.CardModels;
using DevHome.Common.Models;
using DevHome.Common.Renderers;
using DevHome.Common.Views;
using DevHome.Common.Windows;
using DevHome.Contracts.Services;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.ViewModels.Environments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views.Environments;

public sealed partial class EnvironmentCreationOptionsView : UserControl
{
    public EnvironmentCreationOptionsViewModel ViewModel => (EnvironmentCreationOptionsViewModel)this.DataContext;

    public EnvironmentCreationOptionsView()
    {
        this.InitializeComponent();
    }

    private void ViewUnloaded(object sender, RoutedEventArgs e)
    {
        CreationOptionsAdaptiveCardUI.Content = null;
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}
