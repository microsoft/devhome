// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
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
