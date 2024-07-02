// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Views;
using DevHome.Settings.ViewModels;

namespace DevHome.Settings.Views;

public sealed partial class SettingsPage : ToolPage
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = new SettingsViewModel();
        this.InitializeComponent();
    }
}
