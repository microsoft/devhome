// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Extensions;
using DevHome.Customization.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DevHome.Customization.Views;

public sealed partial class MainPageView : UserControl
{
    public MainPageViewModel ViewModel
    {
        get;
    }

    public MainPageView()
    {
        InitializeComponent();

        ViewModel = Application.Current.GetService<MainPageViewModel>();
        this.DataContext = ViewModel;
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadViewModelContentAsync();

        // Focus on the first focusable element inside the shell content
        var element = FocusManager.FindFirstFocusableElement(CustomizationMainPage);
        if (element != null)
        {
            await FocusManager.TryFocusAsync(element, FocusState.Programmatic).AsTask();
        }
    }
}
