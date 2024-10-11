// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public sealed partial class SetupTargetView : UserControl
{
    public SetupTargetViewModel ViewModel => (SetupTargetViewModel)this.DataContext;

    public SetupTargetView()
    {
        this.InitializeComponent();
    }

    private async void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.Initialize(NotificationQueue);
    }
}
