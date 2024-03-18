// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Customization.ViewModels;
using DevHome.Settings.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.Views;

public sealed partial class DeveloperFileExplorerPage : Page
{
    public DeveloperFileExplorerViewModel ViewModel
    {
        get;
    }

    public DeveloperFileExplorerPage()
    {
        ViewModel = Application.Current.GetService<DeveloperFileExplorerViewModel>();
        this.InitializeComponent();
    }
}
