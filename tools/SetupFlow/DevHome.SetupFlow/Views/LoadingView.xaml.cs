// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public sealed partial class LoadingView : UserControl
{
    public LoadingView()
    {
        this.InitializeComponent();
    }

    public LoadingViewModel ViewModel => (LoadingViewModel)this.DataContext;

    private void ContentControl_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Remove the binding for the adaptivecard panel when we leave the page.
        // this prevents crashes due to the stackpanel attempting to be binded to multiple parents.
        if (sender is ContentControl contentControl)
        {
            contentControl.Content = null;
        }
    }
}
