// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Linq;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.ViewModels;
using DevHome.Dashboard.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Controls;
public sealed partial class WidgetControl : UserControl
{
    private MenuFlyoutItem _currentSelectedSize;

    public WidgetViewModel WidgetSource
    {
        get => (WidgetViewModel)GetValue(WidgetSourceProperty);
        set
        {
            SetValue(WidgetSourceProperty, value);
            if (WidgetSource != null)
            {
                UpdateWidgetHeaderIconFill();
            }
        }
    }

    public static readonly DependencyProperty WidgetSourceProperty = DependencyProperty.Register(
        nameof(WidgetSource), typeof(WidgetViewModel), typeof(WidgetControl), new PropertyMetadata(null));

    public WidgetControl()
    {
        this.InitializeComponent();
        ActualThemeChanged += OnActualThemeChanged;
    }

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

                    AddSizesToWidgetMenu(widgetMenuFlyout, widgetViewModel, resourceLoader);
                    widgetMenuFlyout.Items.Add(new MenuFlyoutSeparator());
                    AddCustomizeToWidgetMenu(widgetMenuFlyout, widgetViewModel, resourceLoader);
                    AddRemoveToWidgetMenu(widgetMenuFlyout, widgetViewModel, resourceLoader);
                }
            }
        }
    }

    private void AddRemoveToWidgetMenu(MenuFlyout widgetMenuFlyout, WidgetViewModel widgetViewModel, ResourceLoader resourceLoader)
    {
        var removeWidgetText = resourceLoader.GetString("RemoveWidgetMenuText");
        var icon = new FontIcon()
        {
            Glyph = "\xE77A",
        };
        var menuItemClose = new MenuFlyoutItem
        {
            Tag = widgetViewModel,
            Text = removeWidgetText,
            Icon = icon,
        };
        menuItemClose.Click += OnRemoveWidgetClick;
        menuItemClose.SetValue(AutomationProperties.AutomationIdProperty, "RemoveWidgetButton");
        widgetMenuFlyout.Items.Add(menuItemClose);
    }

    private async void OnRemoveWidgetClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem deleteMenuItem)
        {
            if (deleteMenuItem?.Tag is WidgetViewModel widgetViewModel)
            {
                // Remove any custom state from the widget. In case the deletion fails, we won't show the widget anymore.
                await widgetViewModel.Widget.SetCustomStateAsync(string.Empty);

                // Remove the widget from the list before deleting, otherwise the widget will
                // have changed and the collection won't be able to find it to remove it.
                var widgetIdToDelete = widgetViewModel.Widget.Id;
                var widgetToDelete = widgetViewModel.Widget;
                Log.Logger()?.ReportDebug("WidgetControl", $"User removed widget, delete widget {widgetIdToDelete}");
                DashboardView.PinnedWidgets.Remove(widgetViewModel);
                try
                {
                    await widgetToDelete.DeleteAsync();
                    Log.Logger()?.ReportInfo("WidgetControl", $"Deleted Widget {widgetIdToDelete}");
                }
                catch (Exception ex)
                {
                    Log.Logger()?.ReportError("WidgetControl", $"Didn't delete Widget {widgetIdToDelete}", ex);
                }
            }
        }
    }

    private void AddSizesToWidgetMenu(MenuFlyout widgetMenuFlyout, WidgetViewModel widgetViewModel, ResourceLoader resourceLoader)
    {
        var widgetDefinition = WidgetCatalog.GetDefault().GetWidgetDefinition(widgetViewModel.Widget.DefinitionId);
        var capabilities = widgetDefinition.GetWidgetCapabilities();

        // Add the three possible sizes. Each side should only be enabled if it is included in the widget's capabilities.
        var menuItemSmall = new MenuFlyoutItem
        {
            Tag = WidgetSize.Small,
            Text = resourceLoader.GetString("SmallWidgetMenuText"),
            IsEnabled = capabilities.Any(cap => cap.Size == WidgetSize.Small),
        };
        menuItemSmall.Click += OnMenuItemSizeClick;
        widgetMenuFlyout.Items.Add(menuItemSmall);

        var menuItemMedium = new MenuFlyoutItem
        {
            Tag = WidgetSize.Medium,
            Text = resourceLoader.GetString("MediumWidgetMenuText"),
            IsEnabled = capabilities.Any(cap => cap.Size == WidgetSize.Medium),
        };
        menuItemMedium.Click += OnMenuItemSizeClick;
        widgetMenuFlyout.Items.Add(menuItemMedium);

        var menuItemLarge = new MenuFlyoutItem
        {
            Tag = WidgetSize.Large,
            Text = resourceLoader.GetString("LargeWidgetMenuText"),
            IsEnabled = capabilities.Any(cap => cap.Size == WidgetSize.Large),
        };
        menuItemLarge.Click += OnMenuItemSizeClick;
        widgetMenuFlyout.Items.Add(menuItemLarge);

        // Mark current widget size.
        var size = widgetViewModel.WidgetSize;
        switch (size)
        {
            case WidgetSize.Small:
                _currentSelectedSize = menuItemSmall;
                break;
            case WidgetSize.Medium:
                _currentSelectedSize = menuItemMedium;
                break;
            case WidgetSize.Large:
                _currentSelectedSize = menuItemLarge;
                break;
        }

        MarkSize(_currentSelectedSize);
    }

    private async void OnMenuItemSizeClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuSizeItem)
        {
            if (menuSizeItem.DataContext is WidgetViewModel widgetViewModel)
            {
                // Unset mark on current size.
                if (_currentSelectedSize is not null)
                {
                    _currentSelectedSize.Icon = null;
                }

                // Resize widget.
                var size = (WidgetSize)menuSizeItem.Tag;
                widgetViewModel.WidgetSize = size;
                await widgetViewModel.Widget.SetSizeAsync(size);

                // Set mark on new size.
                _currentSelectedSize = menuSizeItem;
                MarkSize(_currentSelectedSize);
            }
        }
    }

    private void MarkSize(MenuFlyoutItem menuSizeItem)
    {
        var fontIcon = new FontIcon
        {
            Glyph = "\xE915",
        };
        menuSizeItem.Icon = fontIcon;
    }

    private void AddCustomizeToWidgetMenu(MenuFlyout widgetMenuFlyout, WidgetViewModel widgetViewModel, ResourceLoader resourceLoader)
    {
        var customizeWidgetText = resourceLoader.GetString("CustomizeWidgetMenuText");
        var icon = new FontIcon()
        {
            Glyph = "\xE70F",
        };
        var menuItemCustomize = new MenuFlyoutItem
        {
            Tag = widgetViewModel,
            Text = customizeWidgetText,
            Icon = icon,
        };
        menuItemCustomize.Click += OnCustomizeWidgetClick;
        widgetMenuFlyout.Items.Add(menuItemCustomize);
    }

    private void OnCustomizeWidgetClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem customizeMenuItem)
        {
            if (customizeMenuItem?.Tag is WidgetViewModel widgetViewModel)
            {
                widgetViewModel.IsInEditMode = true;
            }
        }
    }

    private async void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        WidgetHeaderIcon.Fill = await WidgetIconCache.GetBrushForWidgetIcon(WidgetSource.WidgetDefinition, ActualTheme);
    }

    private async void UpdateWidgetHeaderIconFill()
    {
        WidgetHeaderIcon.Fill = await WidgetIconCache.GetBrushForWidgetIcon(WidgetSource.WidgetDefinition, ActualTheme);
    }
}
