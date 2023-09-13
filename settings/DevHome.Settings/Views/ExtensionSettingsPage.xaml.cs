// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Settings.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Settings.Views;

public sealed partial class ExtensionSettingsPage : Page
{
    public ExtensionSettingsViewModel ViewModel
    {
        get;
    }

    public ExtensionSettingsPage()
    {
        ViewModel = Application.Current.GetService<ExtensionSettingsViewModel>();
        this.InitializeComponent();
    }

    private void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        ViewModel.BreadcrumbBar_ItemClicked(sender, args);
    }
}
