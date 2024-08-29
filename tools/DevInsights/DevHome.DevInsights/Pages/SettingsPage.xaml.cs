// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.DevInsights.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.DevInsights.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPageViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = new SettingsPageViewModel();
        InitializeComponent();
    }
}
