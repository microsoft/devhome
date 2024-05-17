// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Models;
using DevHome.Settings.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Settings.Views;

public sealed partial class SettingsPage : AutoFocusPage
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = new SettingsViewModel();
        this.InitializeComponent();
    }
}
