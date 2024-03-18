// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using DevHome.Common;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Customization.ViewModels;
using DevHome.Settings.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.Views;

public sealed partial class MainPage : ToolPage
{
    public override string ShortName => "Windows customization";

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
