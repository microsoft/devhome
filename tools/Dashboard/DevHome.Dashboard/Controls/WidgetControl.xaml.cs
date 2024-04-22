// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Dashboard.ComSafeWidgetObjects;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.Services;
using DevHome.Dashboard.ViewModels;
using DevHome.Dashboard.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Widgets;
using Serilog;
using Windows.UI.ViewManagement;
using WinUIEx;

namespace DevHome.Dashboard.Controls;

[ObservableObject]
public sealed partial class WidgetControl : UserControl
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetControl));

    private readonly UISettings _uiSettings = new();

    private readonly StringResource _stringResource;

    private readonly double _headerHeightUnscaled = 36;

    private SelectableMenuFlyoutItem _currentSelectedSize;

    [ObservableProperty]
    private GridLength _headerHeight;

    [ObservableProperty]
    private double _widgetHeight;

    [ObservableProperty]
    private double _widgetWidth;

    public WidgetViewModel WidgetSource
    {
        get => (WidgetViewModel)GetValue(WidgetSourceProperty);
        set
        {
            SetValue(WidgetSourceProperty, value);
            if (WidgetSource != null)
            {
                SetScaledWidthAndHeight(_uiSettings.TextScaleFactor);

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
        _stringResource = new StringResource("DevHome.Dashboard.pri", "DevHome.Dashboard/Resources");
        ActualThemeChanged += OnActualThemeChanged;
    }

    [RelayCommand]
    private void OnLoaded()
    {
        _uiSettings.TextScaleFactorChanged += HandleTextScaleFactorChanged;
    }

    [RelayCommand]
    private void OnUnloaded()
    {
        _uiSettings.TextScaleFactorChanged -= HandleTextScaleFactorChanged;
    }

    private void HandleTextScaleFactorChanged(UISettings sender, object args)
    {
        try
        {
            if (WidgetSource == null)
            {
                return;
            }

            SetScaledWidthAndHeight(sender.TextScaleFactor);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to handle text scale factor changed.");
        }
    }

    private static double GetPixelHeightFromWidgetSize(WidgetSize size)
    {
        return size switch
        {
            WidgetSize.Small => WidgetHelpers.WidgetPxHeightSmall,
            WidgetSize.Medium => WidgetHelpers.WidgetPxHeightMedium,
            WidgetSize.Large => WidgetHelpers.WidgetPxHeightLarge,
            _ => 0,
        };
    }

    private void SetScaledWidthAndHeight(double textScale)
    {
        Application.Current.GetService<WindowEx>().DispatcherQueue.EnqueueAsync(() =>
        {
            HeaderHeight = new GridLength(_headerHeightUnscaled * textScale);
            WidgetHeight = GetPixelHeightFromWidgetSize(WidgetSource.WidgetSize) * textScale;
            WidgetWidth = WidgetHelpers.WidgetPxWidth * textScale;
        });
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
                    await AddSizesToWidgetMenuAsync(widgetMenuFlyout, widgetViewModel);
                    widgetMenuFlyout.Items.Add(new MenuFlyoutSeparator());
                    AddCustomizeToWidgetMenu(widgetMenuFlyout, widgetViewModel);
                    AddRemoveToWidgetMenu(widgetMenuFlyout, widgetViewModel);
                }
            }
        }
    }

    private void AddRemoveToWidgetMenu(MenuFlyout widgetMenuFlyout, WidgetViewModel widgetViewModel)
    {
        var removeWidgetText = _stringResource.GetLocalized("RemoveWidgetMenuText");
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
                _log.Debug($"User removed widget, delete widget {widgetIdToDelete}");
                var stringResource = new StringResource("DevHome.Dashboard.pri", "DevHome.Dashboard/Resources");
                Application.Current.GetService<IScreenReaderService>().Announce(stringResource.GetLocalized("WidgetRemoved"));
                DashboardView.PinnedWidgets.Remove(widgetViewModel);
                try
                {
                    await widgetToDelete.DeleteAsync();
                    _log.Information($"Deleted Widget {widgetIdToDelete}");
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Didn't delete Widget {widgetIdToDelete}");
                }
            }
        }
    }

    private async Task AddSizesToWidgetMenuAsync(MenuFlyout widgetMenuFlyout, WidgetViewModel widgetViewModel)
    {
        var unsafeWidgetDefinition = await Application.Current.GetService<IWidgetHostingService>().GetWidgetDefinitionAsync(widgetViewModel.Widget.DefinitionId);
        if (unsafeWidgetDefinition == null)
        {
            // If we can't get the widgetDefinition, bail and don't show sizes.
            return;
        }

        var widgetDefinitionId = await ComSafeWidgetDefinition.GetIdFromUnsafeWidgetDefinitionAsync(unsafeWidgetDefinition);
        if (string.IsNullOrEmpty(widgetDefinitionId))
        {
            // If we can't get the widgetDefinitionId, bail and don't show sizes.
            return;
        }

        var comSafeWidgetDefinition = new ComSafeWidgetDefinition(widgetDefinitionId);
        if (!await comSafeWidgetDefinition.Populate())
        {
            // If we can't populate the widgetDefinition, bail and don't show sizes.
            return;
        }

        var capabilities = await comSafeWidgetDefinition.GetWidgetCapabilitiesAsync();
        var sizeMenuItems = new List<SelectableMenuFlyoutItem>();

        // Add the three possible sizes. Each side should only be enabled if it is included in the widget's capabilities.
        if (capabilities.Any(cap => cap.Size == WidgetSize.Small))
        {
            var menuItemSmall = new SelectableMenuFlyoutItem
            {
                Tag = WidgetSize.Small,
                Text = _stringResource.GetLocalized("SmallWidgetMenuText"),
            };
            menuItemSmall.Click += OnMenuItemSizeClick;
            widgetMenuFlyout.Items.Add(menuItemSmall);
            sizeMenuItems.Add(menuItemSmall);
        }

        if (capabilities.Any(cap => cap.Size == WidgetSize.Medium))
        {
            var menuItemMedium = new SelectableMenuFlyoutItem
            {
                Tag = WidgetSize.Medium,
                Text = _stringResource.GetLocalized("MediumWidgetMenuText"),
            };
            menuItemMedium.Click += OnMenuItemSizeClick;
            widgetMenuFlyout.Items.Add(menuItemMedium);
            sizeMenuItems.Add(menuItemMedium);
        }

        if (capabilities.Any(cap => cap.Size == WidgetSize.Large))
        {
            var menuItemLarge = new SelectableMenuFlyoutItem
            {
                Tag = WidgetSize.Large,
                Text = _stringResource.GetLocalized("LargeWidgetMenuText"),
            };
            menuItemLarge.Click += OnMenuItemSizeClick;
            widgetMenuFlyout.Items.Add(menuItemLarge);
            sizeMenuItems.Add(menuItemLarge);
        }

        // Mark current widget size.
        _currentSelectedSize = sizeMenuItems.FirstOrDefault(x => (WidgetSize)x.Tag == widgetViewModel.WidgetSize);
        MarkSize(_currentSelectedSize);
    }

    private async void OnMenuItemSizeClick(object sender, RoutedEventArgs e)
    {
        if (sender is SelectableMenuFlyoutItem menuSizeItem)
        {
            if (menuSizeItem.DataContext is WidgetViewModel widgetViewModel)
            {
                // Unset mark on current size.
                if (_currentSelectedSize is not null)
                {
                    _currentSelectedSize.Icon = null;
                    var peer = FrameworkElementAutomationPeer.FromElement(_currentSelectedSize) as SelectableMenuFlyoutItemAutomationPeer;
                    peer.RemoveFromSelection();
                }

                // Resize widget.
                var size = (WidgetSize)menuSizeItem.Tag;
                widgetViewModel.WidgetSize = size;
                await widgetViewModel.Widget.SetSizeAsync(size);
                SetScaledWidthAndHeight(_uiSettings.TextScaleFactor);

                // Set mark on new size.
                _currentSelectedSize = menuSizeItem;
                MarkSize(_currentSelectedSize);
            }
        }
    }

    private void MarkSize(SelectableMenuFlyoutItem menuSizeItem)
    {
        var fontIcon = new FontIcon
        {
            Glyph = "\xE915",
        };
        menuSizeItem.Icon = fontIcon;
        var peer = FrameworkElementAutomationPeer.FromElement(menuSizeItem) as SelectableMenuFlyoutItemAutomationPeer;
        peer.AddToSelection();
    }

    private void AddCustomizeToWidgetMenu(MenuFlyout widgetMenuFlyout, WidgetViewModel widgetViewModel)
    {
        if (widgetViewModel.IsCustomizable)
        {
            var customizeWidgetText = _stringResource.GetLocalized("CustomizeWidgetMenuText");
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
