// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.Services;
using DevHome.Dashboard.ViewModels;
using DevHome.Dashboard.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.Widgets;

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
                // When the WidgetViewModel is updated, the widget icon must also be also updated.
                // Since the icon update must happen asynchronously on the UI thread, it must be
                // called in code rather than binding.
                UpdateWidgetHeaderIconFillAsync();
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

    private async void OpenWidgetMenuAsync(object sender, RoutedEventArgs e)
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

                    try
                    {
                        await AddSizesToWidgetMenuAsync(widgetMenuFlyout, widgetViewModel, resourceLoader);
                        widgetMenuFlyout.Items.Add(new MenuFlyoutSeparator());
                        AddCustomizeToWidgetMenu(widgetMenuFlyout, widgetViewModel, resourceLoader);
                        AddRemoveToWidgetMenu(widgetMenuFlyout, widgetViewModel, resourceLoader);
                    }
                    catch (COMException ex)
                    {
                        Log.Logger()?.ReportError("WidgetControl", $"OpenWidgetMenu", ex);
                        Application.Current.GetService<DashboardViewModel>().DashboardNeedsRestart = true;
                    }
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

    private async Task AddSizesToWidgetMenuAsync(MenuFlyout widgetMenuFlyout, WidgetViewModel widgetViewModel, ResourceLoader resourceLoader)
    {
        var widgetCatalog = await Application.Current.GetService<IWidgetHostingService>().GetWidgetCatalogAsync();
        var widgetDefinition = await Task.Run(() => widgetCatalog.GetWidgetDefinition(widgetViewModel.Widget.DefinitionId));
        var capabilities = widgetDefinition.GetWidgetCapabilities();
        var sizeMenuItems = new List<MenuFlyoutItem>();

        // Add the three possible sizes. Each side should only be enabled if it is included in the widget's capabilities.
        if (capabilities.Any(cap => cap.Size == WidgetSize.Small))
        {
            var menuItemSmall = new MenuFlyoutItem
            {
                Tag = WidgetSize.Small,
                Text = resourceLoader.GetString("SmallWidgetMenuText"),
            };
            menuItemSmall.Click += OnMenuItemSizeClick;
            widgetMenuFlyout.Items.Add(menuItemSmall);
            sizeMenuItems.Add(menuItemSmall);
        }

        if (capabilities.Any(cap => cap.Size == WidgetSize.Medium))
        {
            var menuItemMedium = new MenuFlyoutItem
            {
                Tag = WidgetSize.Medium,
                Text = resourceLoader.GetString("MediumWidgetMenuText"),
            };
            menuItemMedium.Click += OnMenuItemSizeClick;
            widgetMenuFlyout.Items.Add(menuItemMedium);
            sizeMenuItems.Add(menuItemMedium);
        }

        if (capabilities.Any(cap => cap.Size == WidgetSize.Large))
        {
            var menuItemLarge = new MenuFlyoutItem
            {
                Tag = WidgetSize.Large,
                Text = resourceLoader.GetString("LargeWidgetMenuText"),
            };
            menuItemLarge.Click += OnMenuItemSizeClick;
            widgetMenuFlyout.Items.Add(menuItemLarge);
            sizeMenuItems.Add(menuItemLarge);
        }

        // Mark current widget size.
        _currentSelectedSize = sizeMenuItems.FirstOrDefault(x => (WidgetSize)x.Tag == widgetViewModel.WidgetSize);
        MarkSize(_currentSelectedSize);
        return;
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
                    _currentSelectedSize.ClearValue(AutomationProperties.ItemStatusProperty);
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
        var resourceLoader = new ResourceLoader("DevHome.Dashboard.pri", "DevHome.Dashboard/Resources");
        var fontIcon = new FontIcon
        {
            Glyph = "\xE915",
        };
        menuSizeItem.Icon = fontIcon;
        menuSizeItem.SetValue(AutomationProperties.ItemStatusProperty, resourceLoader.GetString("WidgetSizeSelected"));
    }

    private void AddCustomizeToWidgetMenu(MenuFlyout widgetMenuFlyout, WidgetViewModel widgetViewModel, ResourceLoader resourceLoader)
    {
        if (widgetViewModel.IsCustomizable)
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
    }

    private async void OnCustomizeWidgetClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem customizeMenuItem)
        {
            if (customizeMenuItem?.Tag is WidgetViewModel widgetViewModel)
            {
                await widgetViewModel.Widget.NotifyCustomizationRequestedAsync();
            }
        }
    }

    private async void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        WidgetHeaderIcon.Fill = await Application.Current.GetService<IWidgetIconService>()
            .GetBrushForWidgetIconAsync(WidgetSource.WidgetDefinition, ActualTheme);
    }

    private async void UpdateWidgetHeaderIconFillAsync()
    {
        WidgetHeaderIcon.Fill = await Application.Current.GetService<IWidgetIconService>()
            .GetBrushForWidgetIconAsync(WidgetSource.WidgetDefinition, ActualTheme);
    }
}
