// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Customization.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.Views;

public sealed partial class VirtualMachineManagementPage : Page
{
    public VirtualMachineManagementViewModel ViewModel
    {
        get;
    }

    public VirtualMachineManagementPage()
    {
        ViewModel = Application.Current.GetService<VirtualMachineManagementViewModel>();
        this.InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.Initialize(NotificationQueue);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.Uninitialize();
    }
}
