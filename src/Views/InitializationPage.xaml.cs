// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Views;

public sealed partial class InitializationPage : Page
{
    public InitializationViewModel ViewModel
    {
        get;
    }

    public InitializationPage(InitializationViewModel initializationViewModel)
    {
        this.InitializeComponent();
        ViewModel = initializationViewModel;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.OnPageLoaded();
    }
}
