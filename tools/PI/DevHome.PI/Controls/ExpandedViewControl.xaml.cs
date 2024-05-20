// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Extensions;
using DevHome.PI.Properties;
using DevHome.PI.SettingsUi;
using DevHome.PI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.PI.Controls;

public sealed partial class ExpandedViewControl : UserControl
{
    private readonly ExpandedViewControlViewModel viewModel = new();

    public ExpandedViewControl()
    {
        InitializeComponent();
        viewModel.NavigationService.Frame = PageFrame;
    }

    public Frame GetPageFrame()
    {
        return PageFrame;
    }

    public void NavigateTo(Type viewModelType)
    {
        viewModel.NavigateTo(viewModelType);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsToolWindow settingsTool = new(Settings.Default.SettingsToolPosition, null);
        settingsTool.Activate();
    }
}
