// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using DevHome.PI.Properties;
using DevHome.PI.SettingsUi;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Win32.Foundation;
using WinUIEx;

namespace DevHome.PI.ViewModels;

public partial class BarWindowViewModel : ObservableObject
{
    private const string _UnsnapButtonText = "\ue89f";
    private const string _SnapButtonText = "\ue8a0";
    private const string _UnregisterButtonText = "\uECC9";

    private readonly string _errorTitleText = CommonHelper.GetLocalizedString("ToolLaunchErrorTitle");
    private readonly string _errorMessageText = CommonHelper.GetLocalizedString("ToolLaunchErrorMessage");
    private readonly string _pinMenuItemText = CommonHelper.GetLocalizedString("PinMenuItemText");
    private readonly string _unpinMenuItemText = CommonHelper.GetLocalizedString("UnpinMenuItemRawText");
    private readonly string _unregisterMenuItemText = CommonHelper.GetLocalizedString("UnregisterMenuItemRawText");

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private readonly ObservableCollection<MenuFlyoutItem> _externalToolsMenuItems = [];

    public ReadOnlyObservableCollection<MenuFlyoutItem> ExternalToolsMenuItems { get; private set; }

    [ObservableProperty]
    private string _systemCpuUsage = string.Empty;

    [ObservableProperty]
    private string _systemRamUsage = string.Empty;

    [ObservableProperty]
    private string _systemDiskUsage = string.Empty;

    [ObservableProperty]
    private bool _isSnappingEnabled = false;

    [ObservableProperty]
    private string _currentSnapButtonText = _SnapButtonText;

    [ObservableProperty]
    private string _appCpuUsage = string.Empty;

    [ObservableProperty]
    private Visibility _appBarVisibility = Visibility.Visible;

    [ObservableProperty]
    private int _applicationPid;

    [ObservableProperty]
    private SoftwareBitmapSource? _applicationIcon;

    [ObservableProperty]
    private Orientation _barOrientation = Orientation.Horizontal;

    [ObservableProperty]
    private bool _isSnapped;

    [ObservableProperty]
    private bool _showingExpandedContent;

    public BarWindowViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        IsSnappingEnabled = TargetAppData.Instance.HWnd != HWND.Null;
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;

        PerfCounters.Instance.PropertyChanged += PerfCounterHelper_PropertyChanged;

        SystemCpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", PerfCounters.Instance.SystemCpuUsage);
        SystemRamUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormatNoLabelGB", PerfCounters.Instance.SystemRamUsageInGB);
        SystemDiskUsage = CommonHelper.GetLocalizedString("DiskPerfPercentUsageTextFormatNoLabel", PerfCounters.Instance.SystemDiskUsage);

        var process = TargetAppData.Instance.TargetProcess;

        AppBarVisibility = process is null ? Visibility.Collapsed : Visibility.Visible;

        if (process != null)
        {
            ApplicationPid = process.Id;
            ApplicationIcon = TargetAppData.Instance.Icon;
        }

        CurrentSnapButtonText = IsSnapped ? _UnsnapButtonText : _SnapButtonText;

        ExternalToolsMenuItems = new(_externalToolsMenuItems);
        InitializeExternalTools();
    }

    partial void OnIsSnappedChanged(bool value)
    {
        CurrentSnapButtonText = IsSnapped ? _UnsnapButtonText : _SnapButtonText;
    }

    partial void OnBarOrientationChanged(Orientation value)
    {
        if (value == Orientation.Horizontal)
        {
            // If we were snapped, unsnap
            IsSnapped = false;
        }
        else
        {
            // Don't show expanded content in vertical mode
            ShowingExpandedContent = false;
        }
    }

    [RelayCommand]
    public void SwitchLayoutCommand()
    {
        if (BarOrientation == Orientation.Horizontal)
        {
            BarOrientation = Orientation.Vertical;
        }
        else
        {
            BarOrientation = Orientation.Horizontal;
        }
    }

    [RelayCommand]
    public void PerformSnapCommand()
    {
        if (IsSnapped)
        {
            IsSnapped = false;
        }
        else
        {
            // First need to be in a Vertical layout
            BarOrientation = Orientation.Vertical;
            IsSnapped = true;
        }
    }

    [RelayCommand]
    public void ShowBigWindowCommand()
    {
        if (!ShowingExpandedContent)
        {
            // First need to be in a horizontal layout
            BarOrientation = Orientation.Horizontal;
            ShowingExpandedContent = true;
        }
        else
        {
            ShowingExpandedContent = false;
        }
    }

    [RelayCommand]
    public void ProcessChooserCommand()
    {
        // Need to be in a horizontal layout
        BarOrientation = Orientation.Horizontal;

        // And show expanded content
        ShowingExpandedContent = true;

        // And navigate to the appropriate page
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        barWindow?.NavigateTo(typeof(ProcessListPageViewModel));
    }

    private void TargetApp_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TargetAppData.HWnd))
        {
            IsSnappingEnabled = TargetAppData.Instance.HWnd != HWND.Null;
        }
        else if (e.PropertyName == nameof(TargetAppData.TargetProcess))
        {
            var process = TargetAppData.Instance.TargetProcess;

            _dispatcher.TryEnqueue(() =>
            {
                // The App status bar is only visibile if we're attached to a process
                AppBarVisibility = process is null ? Visibility.Collapsed : Visibility.Visible;

                if (process is not null)
                {
                    ApplicationPid = process.Id;
                }
            });
        }
        else if (e.PropertyName == nameof(TargetAppData.Icon))
        {
            SoftwareBitmapSource? icon = TargetAppData.Instance?.Icon;

            _dispatcher.TryEnqueue(() =>
            {
                ApplicationIcon = icon;
            });
        }
        else if (e.PropertyName == nameof(TargetAppData.HasExited))
        {
            // Grey ourselves out if the app has exited?
        }
    }

    private void PerfCounterHelper_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PerfCounters.SystemCpuUsage))
        {
            _dispatcher.TryEnqueue(() =>
            {
                SystemCpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", PerfCounters.Instance.SystemCpuUsage);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.SystemRamUsageInGB))
        {
            _dispatcher.TryEnqueue(() =>
            {
                // Convert from bytes to GBs
                SystemRamUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormatNoLabelGB", PerfCounters.Instance.SystemRamUsageInGB);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.SystemDiskUsage))
        {
            _dispatcher.TryEnqueue(() =>
            {
                SystemDiskUsage = CommonHelper.GetLocalizedString("DiskPerfPercentUsageTextFormatNoLabel", PerfCounters.Instance.SystemDiskUsage);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.CpuUsage))
        {
            _dispatcher.TryEnqueue(() =>
            {
                AppCpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", PerfCounters.Instance.CpuUsage);
            });
        }
    }

    // External tools
    private void InitializeExternalTools()
    {
        ExternalToolsHelper.Instance.Init();
        _externalToolsMenuItems.Clear();

        foreach (var item in ExternalToolsHelper.Instance.AllExternalTools)
        {
            AddExternalToolToContextMenu(item);
        }

        // We have to cast to INotifyCollectionChanged explicitly because the CollectionChanged
        // event in ReadOnlyObservableCollection is protected.
        ((INotifyCollectionChanged)ExternalToolsHelper.Instance.AllExternalTools).CollectionChanged += ExternalTools_CollectionChanged;
    }

    private void ExternalTools_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _dispatcher.TryEnqueue(() =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
            {
                foreach (ExternalTool item in e.NewItems)
                {
                    AddExternalToolToContextMenu(item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
            {
                foreach (ExternalTool item in e.OldItems)
                {
                    RemoveExternalToolFromContextMenu(item);
                }
            }
        });
    }

    private void AddExternalToolToContextMenu(ExternalTool tool)
    {
        _externalToolsMenuItems.Add(CreateContextMenuItemForTool(tool));

        // You can't databind to MenuFlyoutItem, and the ExternalTool icon image is generated asynchronously,
        // so we'll handle the PropertyChanged event in code, so we can update the icon when it gets set.
        // https://github.com/microsoft/microsoft-ui-xaml/issues/1087
        tool.PropertyChanged += ExternalToolItem_PropertyChanged;
    }

    private void RemoveExternalToolFromContextMenu(ExternalTool tool)
    {
        foreach (var menuItem in _externalToolsMenuItems)
        {
            if (menuItem.Tag == tool)
            {
                tool.PropertyChanged -= ExternalToolItem_PropertyChanged;
                _externalToolsMenuItems.Remove(menuItem);
                break;
            }
        }
    }

    // This creates a Menu Flyout item for an external tool. It also creates a sub-menu item for pinning/unpinning
    // and unregistering the tool.
    private MenuFlyoutItem CreateContextMenuItemForTool(ExternalTool item)
    {
        var imageIcon = new ImageIcon
        {
            Source = item.ToolIcon,
        };

        var menuItem = new MenuFlyoutItem
        {
            Text = item.Name,
            Tag = item,
            Icon = item.MenuIcon,
        };
        menuItem.Click += ExternalToolMenuItem_Click;

        var unPinMenuSubItemItem = new MenuFlyoutItem
        {
            Text = item.IsPinned ? _unpinMenuItemText : _pinMenuItemText,
            Icon = GetFontIcon(item.IsPinned ? CommonHelper.UnpinGlyph : CommonHelper.PinGlyph),
            Tag = item,
        };
        unPinMenuSubItemItem.Click += ExternalToolPinUnpin_Click;

        var unRegisterMenuSubItemItem = new MenuFlyoutItem
        {
            Text = _unregisterMenuItemText,
            Icon = GetFontIcon(_UnregisterButtonText),
            Tag = item,
        };
        unRegisterMenuSubItemItem.Click += UnregisterMenuItem_Click;

        var menuSubItemFlyout = new MenuFlyout();
        menuSubItemFlyout.Items.Add(unPinMenuSubItemItem);
        menuSubItemFlyout.Items.Add(unRegisterMenuSubItemItem);

        menuItem.ContextFlyout = menuSubItemFlyout;

        return menuItem;
    }

    private void ExternalToolItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ExternalTool tool)
        {
            Debug.Assert(false, "What is this?");
            return;
        }

        _dispatcher.TryEnqueue(() =>
        {
            // Update the submenu item for this tool
            foreach (var menuItem in _externalToolsMenuItems)
            {
                if (menuItem.Tag == tool)
                {
                    // Update the name if it's changed
                    menuItem.Text = tool.Name;

                    // Update the icon if we've loaded the external tool image. If the image
                    // changes after the fact, we should delete the menu item and recreate it.
                    // If we don't do that, we seem to hit a XAML crash
                    menuItem.Icon ??= tool.MenuIcon;

                    var menuSubItemFlyout = menuItem.ContextFlyout as MenuFlyout;
                    Debug.Assert(menuSubItemFlyout != null, "Why is this null?");

                    var menuSubItemItem = menuSubItemFlyout.Items[0] as MenuFlyoutItem;
                    Debug.Assert(menuSubItemItem != null, "Why is this null?");

                    menuSubItemItem.Text = tool.IsPinned ? _unpinMenuItemText : _pinMenuItemText;
                    menuSubItemItem.Icon = GetFontIcon(tool.IsPinned ? CommonHelper.UnpinGlyph : CommonHelper.PinGlyph);
                    break;
                }
            }
        });
    }

    public void ExternalToolMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem clickedMenuItem)
        {
            if (clickedMenuItem.Tag is ExternalTool tool)
            {
                InvokeTool(tool, TargetAppData.Instance.TargetProcess?.Id, TargetAppData.Instance.HWnd);
            }
        }
    }

    private void ExternalToolPinUnpin_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem clickedMenuItem)
        {
            if (clickedMenuItem.Tag is ExternalTool tool)
            {
                tool.IsPinned = !tool.IsPinned;
            }
        }
    }

    public void ExternalToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button clickedButton)
        {
            if (clickedButton.Tag is ExternalTool tool)
            {
                InvokeTool(tool, TargetAppData.Instance.TargetProcess?.Id, TargetAppData.Instance.HWnd);
            }
        }
    }

    public void ManageExternalToolsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsToolWindow settingsTool = new(Settings.Default.SettingsToolPosition, SettingsPage.AdditionalTools);
        settingsTool.Show();
    }

    private void InvokeTool(ExternalTool tool, int? id, HWND hWnd)
    {
        var process = tool.Invoke(id, hWnd);
        if (process is null)
        {
            // A ContentDialog only renders in the space its parent occupies. Since the parent is a narrow
            // bar, the dialog doesn't have enough space to render. So, we'll use MessageBox to display errors.
            Windows.Win32.PInvoke.MessageBox(
                HWND.Null, // ThisHwnd,
                string.Format(CultureInfo.CurrentCulture, _errorMessageText, tool.Executable),
                _errorTitleText,
                Windows.Win32.UI.WindowsAndMessaging.MESSAGEBOX_STYLE.MB_ICONERROR);
        }
    }

    public void UnregisterMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var menuFlyoutItem = sender as MenuFlyoutItem;
        Debug.Assert(menuFlyoutItem != null, "Why is this null?");
        var tool = menuFlyoutItem.Tag as ExternalTool;
        Debug.Assert(tool != null, "Why is this null?");

        if (tool is not null)
        {
            tool.UnregisterTool();
        }
    }

    private FontIcon GetFontIcon(string s)
    {
        var icon = new FontIcon();
        icon.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons");
        icon.Glyph = s;

        return icon;
    }
}
