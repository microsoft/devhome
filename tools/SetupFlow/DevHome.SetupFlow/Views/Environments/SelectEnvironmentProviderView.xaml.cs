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

        // OnLoaded finishes execution before the UI updates.  I don't know why.
        // Add a tiny delay to allow the UI to render before setting focus.
        // I tried the AutoFocus behavior from the community toolkit.
        // Same issue.  Focus was set right before the UI rendered the Items control.
        await Task.Delay(100);
        this.Focus(FocusState.Programmatic);
    }
}
