// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using DevHome.SetupFlow.ViewModels.Environments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views.Environments;

public sealed partial class SelectEnvironmentProviderView : UserControl
{
    public SelectEnvironmentProviderViewModel ViewModel => (SelectEnvironmentProviderViewModel)this.DataContext;

    public SelectEnvironmentProviderView()
    {
        this.InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync(NotificationQueue);
    }
}
