// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Customization.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
    }
}
