// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;

namespace DevHome.Dashboard.Views;
public sealed partial class WidgetControl : UserControl
{
    public WidgetControl()
    {
        this.InitializeComponent();
    }

    public WidgetViewModel WidgetSource
    {
        get => (WidgetViewModel)GetValue(WidgetSourceProperty);
        set => SetValue(WidgetSourceProperty, value);
    }

    public static readonly DependencyProperty WidgetSourceProperty = DependencyProperty.Register(
        "WidgetSource", typeof(WidgetViewModel), typeof(WidgetControl), new PropertyMetadata(null));

    private void OpenWidgetMenu(object sender, RoutedEventArgs e)
    {
        if (sender as Button is Button widgetMenuButton)
        {
            var widgetMenuFlyout = widgetMenuButton.Flyout as MenuFlyout;
            widgetMenuFlyout.Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.BottomEdgeAlignedLeft;
            if (widgetMenuFlyout?.Items.Count == 0)
            {
                var widgetControl = widgetMenuButton.Tag as WidgetControl;
                if (widgetControl != null && widgetControl.WidgetSource is WidgetViewModel widgetViewModel)
                {
                    var resourceLoader = new ResourceLoader("DevHome.Dashboard.pri", "DevHome.Dashboard/Resources");
                    var text = resourceLoader.GetString("RemoveWidgetMenuText");
                    var menuItemClose = new MenuFlyoutItem
                    {
                        Tag = widgetViewModel,
                        Text = text,
                    };
                    menuItemClose.Click += DeleteWidgetClick;
                    widgetMenuFlyout.Items.Add(menuItemClose);
                }
            }
        }
    }

    private async void DeleteWidgetClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem deleteMenuItem)
        {
            if (deleteMenuItem?.Tag is WidgetViewModel widgetViewModel)
            {
                // Remove the widget from the list before deleting, otherwise the widget will
                // have changed and the collection won't be able to find it to remove it.
                var widgetIdToDelete = widgetViewModel.Widget.Id;
                Log.Logger()?.ReportDebug("WidgetControl", $"User removed widget, delete widget {widgetIdToDelete}");
                DashboardView.PinnedWidgets.Remove(widgetViewModel);
                await widgetViewModel.Widget.DeleteAsync();
                Log.Logger()?.ReportInfo("WidgetControl", $"Deleted Widget {widgetIdToDelete}");
            }
        }
    }
}
