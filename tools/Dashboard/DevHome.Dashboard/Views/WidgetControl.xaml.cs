// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.Widgets;

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
                    var removeWidgetText = resourceLoader.GetString("RemoveWidgetMenuText");
                    var menuItemClose = new MenuFlyoutItem
                    {
                        Tag = widgetViewModel,
                        Text = removeWidgetText,
                    };
                    menuItemClose.Click += DeleteWidgetClick;
                    widgetMenuFlyout.Items.Add(menuItemClose);

                    AddSizesToWidgetMenu(widgetMenuFlyout, widgetViewModel, resourceLoader);
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

    private void AddSizesToWidgetMenu(MenuFlyout widgetMenuFlyout, WidgetViewModel widgetViewModel, ResourceLoader resourceLoader)
    {
        // Add all three sizes to the menu, but disable them for now.
        var menuItemSmall = new MenuFlyoutItem
        {
            Tag = WidgetSize.Small,
            Text = resourceLoader.GetString("SmallWidgetMenuText"),
            IsEnabled = false,
        };
        menuItemSmall.Click += MenuItemSize_Click;
        widgetMenuFlyout.Items.Add(menuItemSmall);

        var menuItemMedium = new MenuFlyoutItem
        {
            Tag = WidgetSize.Medium,
            Text = resourceLoader.GetString("MediumWidgetMenuText"),
            IsEnabled = false,
        };
        menuItemMedium.Click += MenuItemSize_Click;
        widgetMenuFlyout.Items.Add(menuItemMedium);

        var menuItemLarge = new MenuFlyoutItem
        {
            Tag = WidgetSize.Large,
            Text = resourceLoader.GetString("LargeWidgetMenuText"),
            IsEnabled = false,
        };
        menuItemLarge.Click += MenuItemSize_Click;
        widgetMenuFlyout.Items.Add(menuItemLarge);

        // Enable the sizes that are valid for this widget.
        var widgetDefinition = _widgetCatalog.GetWidgetDefinition(widgetViewModel.Widget.DefinitionId);
        var capabilities = widgetDefinition.GetWidgetCapabilities();

        if (capabilities.Any(cap => cap.Size == WidgetSize.Small))
        {
            menuItemSmall.IsEnabled = true;
        }

        if (capabilities.Any(cap => cap.Size == WidgetSize.Medium))
        {
            menuItemMedium.IsEnabled = true;
        }

        if (capabilities.Any(cap => cap.Size == WidgetSize.Large))
        {
            menuItemLarge.IsEnabled = true;
        }
    }

    private async void MenuItemSize_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuSizeItem)
        {
            if (menuSizeItem.DataContext is WidgetViewModel widgetViewModel)
            {
                var size = (WidgetSize)menuSizeItem.Tag;
                await widgetViewModel.Widget.SetSizeAsync(size);
                widgetViewModel.WidgetSize = size;
            }
        }
    }
}
