// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using DevHome.Common;
using DevHome.Dashboard.ViewModels;
using DevHome.Dashboard.Views;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard;

public partial class DashboardView : ToolPage
{
    public override string ShortName => "Dashboard";

    public DashboardViewModel ViewModel { get; }

    private readonly WidgetHost _widgetHost;
    private readonly WidgetCatalog _widgetCatalog;

    public DashboardView()
    {
        ViewModel = new DashboardViewModel();
        this.InitializeComponent();

        // GUID is your personal Host GUID that widget platform will use to identify you
        _widgetHost = WidgetHost.Register(new WidgetHostContext("BAA93438-9B07-4554-AD09-7ACCD7D4F031"));
        _widgetCatalog = WidgetCatalog.GetDefault();
    }

    private async void AddWidgetButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialog = new AddWidgetDialog(_widgetCatalog)
        {
            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            XamlRoot = this.XamlRoot,
        };
        _ = await dialog.ShowAsync();
    }
}
