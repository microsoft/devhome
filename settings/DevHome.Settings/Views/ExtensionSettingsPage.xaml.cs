// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Services;
using DevHome.Common.Views;
using DevHome.Settings.Models;
using DevHome.Settings.ViewModels;
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
        ViewModel = new ExtensionSettingsViewModel();
        this.InitializeComponent();
    }

    private void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        ViewModel.BreadcrumbBar_ItemClicked(sender, args);
    }
}
