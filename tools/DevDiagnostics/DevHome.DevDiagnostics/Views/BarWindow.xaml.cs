// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using DevHome.Common.Extensions;
using DevHome.DevDiagnostics.Controls;
using DevHome.DevDiagnostics.Helpers;
using DevHome.DevDiagnostics.Models;
using DevHome.DevDiagnostics.Properties;
using DevHome.DevDiagnostics.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;
using WinUIEx;
using static DevHome.DevDiagnostics.Helpers.CommonHelper;
using static DevHome.DevDiagnostics.Helpers.WindowHelper;

namespace DevHome.DevDiagnostics;

public partial class BarWindow : ThemeAwareWindow
{
    private enum PinOption
    {
        Pin,
        UnPin,
    }

    private const string ExpandButtonText = "\ue70d"; // ChevronDown
    private const string CollapseButtonText = "\ue70e"; // ChevronUp
    private const string ManageToolsButtonText = "\uec7a"; // DeveloperTools
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(BarWindow));

    private readonly string _aliasDisabledDialogTitle = GetLocalizedString("AliasDisabledDialogTitle");
    private readonly string _aliasDisabledDialogContent = GetLocalizedString("AliasDisabledDialogContent");
    private readonly string _aliasDisabledDialogButtonText = GetLocalizedString("AliasDisabledDialogButtonText");
    private readonly string _pinMenuItemText = GetLocalizedString("PinMenuItemText");
    private readonly string _unpinMenuItemText = GetLocalizedString("UnpinMenuItemRawText");
    private readonly string _unregisterMenuItemText = GetLocalizedString("UnregisterMenuItemRawText");
    private readonly string _manageToolsMenuItemText = GetLocalizedString("ManageExternalToolsMenuText");

    private readonly Settings _settings = Settings.Default;
    private readonly BarWindowViewModel _viewModel;
    private readonly ExternalToolsHelper _externalTools;
    private readonly InternalToolsHelper _internalTools;

    // Constants that control window sizes
    private const int WindowPositionOffsetY = 30;
    private const int FloatingHorizontalBarHeight = 90;

    // Default size of the expanded view as a percentage of the screen size
    private const float DefaultExpandedViewHeightofScreen = 0.9f;

    private float _previousCustomTitleBarOffset;

    internal HWND ThisHwnd { get; private set; }

    public BarWindow()
    {
        _viewModel = new BarWindowViewModel();
        _externalTools = Application.Current.GetService<ExternalToolsHelper>();
        _internalTools = Application.Current.GetService<InternalToolsHelper>();
        Title = GetLocalizedString("DDDisplayName");

        InitializeComponent();
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        ExpandCollapseLayoutButtonText.Text = _viewModel.ShowingExpandedContent ? CollapseButtonText : ExpandButtonText;
        CustomTitleBarButtons.Add(ExpandCollapseLayoutButton);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BarWindowViewModel.ShowingExpandedContent))
        {
            if (_viewModel.ShowingExpandedContent)
            {
                ExpandLargeContentPanel();
                ExpandCollapseLayoutButtonText.Text = CollapseButtonText;
            }
            else
            {
                CollapseLargeContentPanel();
                ExpandCollapseLayoutButtonText.Text = ExpandButtonText;
            }
        }
    }

    private void MainPanel_Loaded(object sender, RoutedEventArgs e)
    {
        ThisHwnd = (HWND)WindowNative.GetWindowHandle(this);

        _settings.PropertyChanged += Settings_PropertyChanged;

        if (_settings.IsClipboardMonitoringEnabled)
        {
            ClipboardMonitor.Instance.Start();
        }

        if (_settings.IsCpuUsageMonitoringEnabled)
        {
            PerfCounters.Instance.Start();
        }

        // Apply the user's chosen theme setting.
        ThemeName t = ThemeName.Themes.First(t => t.Name == _settings.CurrentTheme);
        SetRequestedTheme(t.Theme);

        SetDefaultPosition();
        SetRegionsForTitleBar();

        PopulateCommandBar();
        ((INotifyCollectionChanged)Application.Current.GetService<ExternalToolsHelper>().AllExternalTools).CollectionChanged += AllExternalTools_CollectionChanged;

        // Now that the position is set correctly show the window
        this.Show();
        HookWndProc();
    }

    public void PopulateCommandBar()
    {
        AddManageToolsOptionToCommandBar();

        foreach (ExternalTool tool in _externalTools.AllExternalTools)
        {
            AddToolToCommandBar(tool);
        }

        foreach (Tool tool in _internalTools.AllInternalTools)
        {
            AddToolToCommandBar(tool);
        }
    }

    private AppBarButton CreateAppBarButton(Tool tool, PinOption pinOption)
    {
        AppBarButton button = new AppBarButton
        {
            Label = tool.Name,
            Tag = tool,
        };

        button.Icon = tool.GetIcon();
        button.Command = tool.InvokeCommand;
        button.CommandParameter = this;
        button.ContextFlyout = CreateMenuFlyout(tool, pinOption);

        ToolTipService.SetToolTip(button, tool.Name);

        return button;
    }

    private MenuFlyout CreateMenuFlyout(Tool tool, PinOption pinOption)
    {
        MenuFlyout menu = new MenuFlyout();
        menu.Items.Add(CreatePinMenuItem(tool, pinOption));
        menu.Items.Add(CreateUnregisterMenuItem(tool));

        return menu;
    }

    private void AddToolToCommandBar(Tool tool)
    {
        // We create 2 copies of the button, one for the primary commands list and one for the secondary commands list.
        // We're not allowed to put the same button in both lists.
        AppBarButton primaryCommandButton = CreateAppBarButton(tool, PinOption.UnPin); // The primary button should always have the unpin option
        AppBarButton secondaryCommandButton = CreateAppBarButton(tool, tool.IsPinned ? PinOption.UnPin : PinOption.Pin); // The secondary button is dynamic

        // If a tool is pinned, we'll add it to the primary commands list.
        if (tool.IsPinned)
        {
            ToolsCommandBar.PrimaryCommands.Add(primaryCommandButton);
        }

        // We'll always add all tools to the secondary commands list.
        ToolsCommandBar.SecondaryCommands.Add(secondaryCommandButton);

        tool.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(Tool.ToolIconSource))
            {
                // An ImageIcon can only be set once, so we can't share it with both buttons
                primaryCommandButton.Icon = tool.GetIcon();
                secondaryCommandButton.Icon = tool.GetIcon();
            }
            else if (args.PropertyName == nameof(Tool.IsPinned))
            {
                // If a tool is pinned, we'll add it to the primary commands list, otherwise the secondary commands list
                secondaryCommandButton.ContextFlyout = CreateMenuFlyout(tool, tool.IsPinned ? PinOption.UnPin : PinOption.Pin);

                if (tool.IsPinned)
                {
                    ToolsCommandBar.PrimaryCommands.Add(primaryCommandButton);
                }
                else
                {
                    ToolsCommandBar.PrimaryCommands.Remove(primaryCommandButton);
                }
            }
        };
    }

    private void AddManageToolsOptionToCommandBar()
    {
        // Put in the "manage tools" button
        AppBarButton manageToolsButton = new AppBarButton
        {
            Label = _manageToolsMenuItemText,
            Icon = new FontIcon() { Glyph = ManageToolsButtonText },
            Command = _viewModel.ManageExternalToolsButtonCommand,
        };

        // This should be at the top of the secondary command list
        ToolsCommandBar.SecondaryCommands.Insert(0, manageToolsButton);
        ToolsCommandBar.SecondaryCommands.Insert(1, new AppBarSeparator());
    }

    private MenuFlyoutItem CreatePinMenuItem(Tool tool, PinOption pinOption)
    {
        MenuFlyoutItem item = new MenuFlyoutItem
        {
            Text = pinOption == PinOption.Pin ? _pinMenuItemText : _unpinMenuItemText,
            Command = tool.TogglePinnedStateCommand,
            Icon = new FontIcon() { Glyph = tool.PinGlyph },
        };

        return item;
    }

    private MenuFlyoutItem CreateUnregisterMenuItem(Tool tool)
    {
        MenuFlyoutItem unRegister = new MenuFlyoutItem
        {
            Text = _unregisterMenuItemText,
            Command = tool.UnregisterToolCommand,
            Icon = new FontIcon() { Glyph = "\uECC9" },
        };

        return unRegister;
    }

    private void AllExternalTools_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            {
                if (e.NewItems is not null)
                {
                    foreach (ExternalTool newItem in e.NewItems)
                    {
                        AddToolToCommandBar(newItem);
                    }
                }

                break;
            }

            case NotifyCollectionChangedAction.Remove:
            {
                Debug.Assert(e.OldItems is not null, "Why is old items null");
                foreach (ExternalTool oldItem in e.OldItems)
                {
                    if (oldItem.IsPinned)
                    {
                        // Find this item in the command bar
                        AppBarButton? pinnedButton = ToolsCommandBar.PrimaryCommands.OfType<AppBarButton>().FirstOrDefault(b => b.Tag == oldItem);
                        if (pinnedButton is not null)
                        {
                            ToolsCommandBar.PrimaryCommands.Remove(pinnedButton);
                        }
                        else
                        {
                            Debug.Assert(false, "Could not find button for tool");
                        }
                    }

                    AppBarButton? button = ToolsCommandBar.SecondaryCommands.OfType<AppBarButton>().FirstOrDefault(b => b.Tag == oldItem);
                    if (button is not null)
                    {
                        ToolsCommandBar.SecondaryCommands.Remove(button);
                    }
                    else
                    {
                        Debug.Assert(false, "Could not find button for tool");
                    }
                }

                break;
            }
        }
    }

    public void TitlebarLayoutUpdate()
    {
        if (AppWindow is null)
        {
            return;
        }

        if (_previousCustomTitleBarOffset != ChromeButtonPanel.ActualOffset.X)
        {
            // If the offset has changed, we need to update the regions for the title bar
            SetRegionsForTitleBar();
            _previousCustomTitleBarOffset = ChromeButtonPanel.ActualOffset.X;
        }
    }

    public void SetRegionsForTitleBar()
    {
        var scaleAdjustment = MainPanel.XamlRoot.RasterizationScale;

        RightPaddingColumn.Width = new GridLength(AppWindow.TitleBar.RightInset / scaleAdjustment);
        LeftPaddingColumn.Width = new GridLength(AppWindow.TitleBar.LeftInset / scaleAdjustment);

        var transform = ChromeButtonPanel.TransformToVisual(null);
        var bounds = transform.TransformBounds(new Rect(0, 0, ChromeButtonPanel.ActualWidth, ChromeButtonPanel.ActualHeight));
        var chromeButtonsRect = GetRect(bounds, scaleAdjustment);
        var rectArray = new Windows.Graphics.RectInt32[] { chromeButtonsRect };

        var nonClientInputSrc = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
        nonClientInputSrc.SetRegionRects(NonClientRegionKind.Passthrough, rectArray);
    }

    private void SetDefaultPosition()
    {
        // First set our default size before setting out position
        SetDefaultWidthAndHeight();

        // If attached to an app it should show up on the monitor that the app is on
        // Be sure to grab the DPI for that monitor
        var dpiScale = GetDpiScaleForWindow(_viewModel.ApplicationHwnd ?? TryGetParentProcessHWND() ?? ThisHwnd);

        RECT monitorRect = GetMonitorRectForWindow(_viewModel.ApplicationHwnd ?? TryGetParentProcessHWND() ?? ThisHwnd);
        var screenWidth = monitorRect.right - monitorRect.left;
        this.Move(
            (int)((screenWidth - (Width * dpiScale)) / 2) + monitorRect.left,
            (int)WindowPositionOffsetY + monitorRect.top);
    }

    internal void SetDefaultWidthAndHeight()
    {
        // Get the saved settings for the ExpandedView size. On first run, this will be
        // the default 0,0, so we'll set the size proportional to the monitor size.
        // Subsequently, it will be whatever size the user sets.
        RECT monitorRect = GetMonitorRectForWindow(_viewModel.ApplicationHwnd ?? TryGetParentProcessHWND() ?? ThisHwnd);
        var dpiScale = GetDpiScaleForWindow(_viewModel.ApplicationHwnd ?? TryGetParentProcessHWND() ?? ThisHwnd);

        var settingWidth = Settings.Default.WindowWidth;
        if (settingWidth == 0)
        {
            settingWidth = monitorRect.Width * 2 / (3 * dpiScale);
            Settings.Default.WindowWidth = settingWidth;
        }

        var settingHeight = Settings.Default.ExpandedWindowHeight;
        if (settingHeight == 0)
        {
            settingHeight = monitorRect.Height * 3 / (4 * dpiScale);
            Settings.Default.ExpandedWindowHeight = settingHeight;
        }

        // Set the default window width
        Width = Settings.Default.WindowWidth;

        // And set the default window height
        if (LargeContentPanel is not null &&
            LargeContentPanel.Visibility == Visibility.Visible &&
            this.WindowState != WindowState.Maximized)
        {
            Height = Settings.Default.ExpandedWindowHeight;
        }
        else
        {
            Height = FloatingHorizontalBarHeight;
        }
    }

    internal void UpdatePositionFromHwnd(HWND hwnd)
    {
        RECT rect;
        PInvoke.GetWindowRect(hwnd, out rect);
        this.Move(rect.left, rect.top);
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        ClipboardMonitor.Instance.Stop();

        // Save window size if we're not maximized
        if (this.WindowState != WindowState.Maximized)
        {
            if (LargeContentPanel is not null &&
                LargeContentPanel.Visibility == Visibility.Visible)
            {
                Settings.Default.ExpandedWindowHeight = Height;
            }

            Settings.Default.WindowWidth = Width;
            Settings.Default.Save();
        }

        TargetAppData.Instance.ClearAppData();
        PerfCounters.Instance.Stop();

        var primaryWindow = Application.Current.GetService<PrimaryWindow>();
        primaryWindow.ClearBarWindow();

        // Unsubscribe from the activation handler
        Activated -= Window_Activated;
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.IsClipboardMonitoringEnabled))
        {
            if (_settings.IsClipboardMonitoringEnabled)
            {
                ClipboardMonitor.Instance.Start();
            }
            else
            {
                ClipboardMonitor.Instance.Stop();
            }
        }
        else if (string.Equals(e.PropertyName, nameof(Settings.IsCpuUsageMonitoringEnabled), StringComparison.Ordinal))
        {
            if (_settings.IsCpuUsageMonitoringEnabled)
            {
                PerfCounters.Instance.Start();
            }
            else
            {
                PerfCounters.Instance.Stop();
            }
        }
    }

    private void ExpandLargeContentPanel()
    {
        // We're expanding.
        // Switch the bar to horizontal before we adjust the size.
        LargeContentPanel.Visibility = Visibility.Visible;

        var monitorRect = GetMonitorRectForWindow(ThisHwnd);
        var dpiScale = GetDpiScaleForWindow(ThisHwnd);

        // If we're maximized, we need to set the height to the monitor height
        if (WindowState == WindowState.Maximized)
        {
            Height = monitorRect.Height / dpiScale;
        }
        else
        {
            // Expand the window but keep the x,y coordinates of top-left most corner of the window the same so it doesn't
            // jump around the screen.
            var availableHeight = monitorRect.Height - Math.Abs(AppWindow.Position.Y - monitorRect.top);
            var targetHeight = (int)((double)availableHeight / dpiScale * DefaultExpandedViewHeightofScreen);

            // Set the height to the smaller of either the cached height or the computed size
            Height = Math.Min(targetHeight, Settings.Default.ExpandedWindowHeight);
        }
    }

    private void CollapseLargeContentPanel()
    {
        // Make sure we cache the state before switching to collapsed bar.
        if (Height > FloatingHorizontalBarHeight)
        {
            Settings.Default.ExpandedWindowHeight = Height;
        }

        LargeContentPanel.Visibility = Visibility.Collapsed;
        this.Height = FloatingHorizontalBarHeight;
        this.MinHeight = FloatingHorizontalBarHeight;
    }

    internal void NavigateTo(Type viewModelType)
    {
        _viewModel.ShowingExpandedContent = true;
        ExpandedViewControl.NavigateTo(viewModelType);
    }

    internal void NavigateToPiSettings(string settingsPage)
    {
        _viewModel.ShowingExpandedContent = true;
        ExpandedViewControl.NavigateToSettings(settingsPage);
    }

    internal Frame GetFrame()
    {
        return ExpandedViewControl.GetPageFrame();
    }

    private void WindowEx_WindowStateChanged(object sender, WindowState e)
    {
        if (e.Equals(WindowState.Normal))
        {
            // If, as part of being restored, we were in an expanded state, then make our window bigger (don't go back to collapsed mode)
            if (_viewModel.ShowingExpandedContent && Height == FloatingHorizontalBarHeight)
            {
                Height = Settings.Default.ExpandedWindowHeight;
            }
        }
        else if (e.Equals(WindowState.Maximized))
        {
            // If we're being maximized, expand our content
            _viewModel.ShowingExpandedContent = true;
        }
    }

    private SUBCLASSPROC? _wndProc;

    private void HookWndProc()
    {
        _wndProc = new SUBCLASSPROC(NewWindowProc);
        PInvoke.SetWindowSubclass(ThisHwnd, _wndProc, 456, 0);
    }

    private bool _isSnapped;
    private bool _transitionFromSnapped;

    private LRESULT NewWindowProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam, nuint uldSubclass, nuint dwRefData)
    {
        switch (msg)
        {
            case PInvoke.WM_WINDOWPOSCHANGING:
            {
                WINDOWPOS wndPos = Marshal.PtrToStructure<WINDOWPOS>(lParam);

                // We only care about this message if it's triggering a resize
                if (wndPos.flags.HasFlag(SET_WINDOW_POS_FLAGS.SWP_NOSIZE))
                {
                    break;
                }

                int floatingBarHeight = CommonHelper.MulDiv(FloatingHorizontalBarHeight, (int)this.GetDpiForWindow(), 96);

                if (PInvoke.IsWindowArranged(hWnd))
                {
                    _isSnapped = true;
                }
                else
                {
                    if (_isSnapped)
                    {
                        _transitionFromSnapped = true;
                        _isSnapped = false;
                    }
                }

                if (wndPos.cy > floatingBarHeight && !_viewModel.ShowingExpandedContent)
                {
                    // We're trying to make our window bigger than the floating bar height and we're in collapsed mode.
                    if (!_isSnapped)
                    {
                        // Enforce our height limit if we're not being snapped
                        wndPos.cy = CommonHelper.MulDiv(FloatingHorizontalBarHeight, (int)this.GetDpiForWindow(), 96);
                        Marshal.StructureToPtr(wndPos, lParam, true);
                        _log.Information("WM_WINDOWPOSCHANGING: Enforcing height limit " + _isSnapped + " " + _transitionFromSnapped);
                    }
                    else
                    {
                        // In the snapped case, let the system make our window bigger, and we'll show the expanded content
                        _viewModel.ShowingExpandedContent = true;
                        _log.Information("WM_WINDOWPOSCHANGING: Enabling expanded content due to large window size");
                    }
                }
                else if (wndPos.cy <= floatingBarHeight && _viewModel.ShowingExpandedContent && _transitionFromSnapped)
                {
                    // We're transitioning from snapped to unsnapped and our window is the size of the floating bar, but we're
                    // trying to show expanded content. We need to expand our window to show the expanded content.
                    wndPos.cy = CommonHelper.MulDiv((int)Settings.Default.ExpandedWindowHeight, (int)this.GetDpiForWindow(), 96);
                    Marshal.StructureToPtr(wndPos, lParam, true);
                    _log.Information("WM_WINDOWPOSCHANGING: Expanding window size for expanded content " + _isSnapped);
                }
                else
                {
                    // We'll say we're done transitioning from snapped once our size is "valid" for our current state
                    _transitionFromSnapped = false;
                }

                break;
            }
        }

        return PInvoke.DefSubclassProc(hWnd, msg, wParam, lParam);
    }

    public void ShowDialogToEnableAppExecutionAlias()
    {
        _ = this.ShowMessageDialogAsync(dialog =>
        {
            dialog.Title = _aliasDisabledDialogTitle;
            dialog.Content = new TextBlock()
            {
                Text = _aliasDisabledDialogContent,
                TextWrapping = TextWrapping.WrapWholeWords,
            };
            dialog.PrimaryButtonText = _aliasDisabledDialogButtonText;
            dialog.PrimaryButtonCommand = _viewModel.LaunchAdvancedAppsPageInWindowsSettingsCommand;
        });
    }
}
