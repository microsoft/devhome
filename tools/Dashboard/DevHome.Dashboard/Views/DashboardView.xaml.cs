// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using DevHome.Common;
using DevHome.Dashboard.ViewModels;
using DevHome.Dashboard.Views;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Dashboard;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public partial class DashboardView : ToolPage
{
    public override string ShortName => "Dashboard";

    public DashboardViewModel ViewModel { get; }

    public DashboardView()
    {
        this.InitializeComponent();
    }

    private async void AddWidgetButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialog = new AddWidgetDialog
        {
            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            XamlRoot = this.XamlRoot,
        };
        _ = await dialog.ShowAsync();
    }
}
