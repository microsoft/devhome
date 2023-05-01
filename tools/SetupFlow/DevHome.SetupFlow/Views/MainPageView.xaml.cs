// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public sealed partial class MainPageView : UserControl
{
    public MainPageView()
    {
        this.InitializeComponent();
    }

    public MainPageViewModel ViewModel => (MainPageViewModel)this.DataContext;

    private async void UpdateAppInstallerButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Title = ViewModel.AppInstallerUpdateAvailableTitle,
            Content = ViewModel.AppInstallerUpdateAvailableMessage,
            PrimaryButtonText = ViewModel.AppInstallerUpdateAvailableUpdateButton,
            PrimaryButtonCommand = ViewModel.UpdateAppInstallerCommand,
            CloseButtonText = ViewModel.AppInstallerUpdateAvailableCancelButton,
            DefaultButton = ContentDialogButton.Primary,
        };
        await dialog.ShowAsync();
    }
}
