// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Customization.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.Views;

public sealed partial class QuietBackgroundProcessesView : UserControl
{
    public QuietBackgroundProcessesViewModel ViewModel
    {
        get;
    }

    public QuietBackgroundProcessesView()
    {
        InitializeComponent();

        ViewModel = Application.Current.GetService<QuietBackgroundProcessesViewModel>();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadViewModelContentAsync();
    }
}
