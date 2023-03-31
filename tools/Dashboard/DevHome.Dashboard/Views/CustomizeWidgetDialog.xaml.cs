// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Views;
public sealed partial class CustomizeWidgetDialog : ContentDialog
{
    public Widget EditedWidget { get; set; }

    public WidgetViewModel ViewModel { get; set; }

    private readonly WidgetDefinition _widgetDefinition;
    private readonly WidgetHost _widgetHost;

    public CustomizeWidgetDialog(WidgetHost host, AdaptiveCardRenderer renderer, DispatcherQueue dispatcher, WidgetDefinition widgetDefinition)
    {
        ViewModel = new WidgetViewModel(null, Microsoft.Windows.Widgets.WidgetSize.Large, null, renderer, dispatcher);
        this.InitializeComponent();

        _widgetHost = host;
        _widgetDefinition = widgetDefinition;

        this.Loaded += InitializeWidgetCustomization;
    }

    private async void InitializeWidgetCustomization(object sender, RoutedEventArgs e)
    {
        var size = WidgetHelpers.GetLargetstCapabilitySize(_widgetDefinition.GetWidgetCapabilities());

        // Create the widget for configuration. We will need to delete it if
        var widget = await _widgetHost.CreateWidgetAsync(_widgetDefinition.Id, size);
        Log.Logger()?.ReportInfo("CustomizeWidgetDialog", $"Created Widget {widget.Id}");

        ViewModel.Widget = widget;
    }

    private void UpdateWidgetButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Logger()?.ReportDebug("CustomizeWidgetDialog", $"Exiting dialog, updated widget");
        EditedWidget = ViewModel.Widget;
        this.Hide();
    }

    private async void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Logger()?.ReportDebug("CustomizeWidgetDialog", $"Exiting dialog, cancel button clicked");
        var widgetIdToDelete = ViewModel.Widget.Id;
        await ViewModel.Widget.DeleteAsync();
        Log.Logger()?.ReportInfo("CustomizeWidgetDialog", $"Deleted Widget {widgetIdToDelete}");

        EditedWidget = null;
        this.Hide();
    }
}
