// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Customization.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.Views;

public sealed partial class VirtualizationFeatureManagementPage : Page
{
    public VirtualizationFeatureManagementViewModel ViewModel
    {
        get;
    }

    public VirtualizationFeatureManagementPage()
    {
        ViewModel = Application.Current.GetService<VirtualizationFeatureManagementViewModel>();
        this.InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.Initialize(NotificationQueue);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.Uninitialize();
    }
}
