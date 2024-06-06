// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.PI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.PI.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPageViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = new SettingsPageViewModel();
        InitializeComponent();
    }
}
