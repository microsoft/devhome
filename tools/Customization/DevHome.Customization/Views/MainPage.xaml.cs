// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Views;
using DevHome.Customization.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.Customization.Views;

public sealed partial class MainPage : ToolPage
{
    public MainPageViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = Application.Current.GetService<MainPageViewModel>();
        InitializeComponent();
    }
}
