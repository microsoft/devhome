// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using DevHome.Common.Extensions;
using DevHome.PI.Controls;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using DevHome.PI.Properties;
using DevHome.PI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinRT.Interop;
using WinUIEx;
using static DevHome.PI.Helpers.CommonHelper;
using static DevHome.PI.Helpers.WindowHelper;

namespace DevHome.PI;

public partial class BarWindowHorizontal : WindowEx
{
    private enum PinOption
    {
        Pin,
        UnPin,
    }

    private const string ExpandButtonText = "\ue70d"; // ChevronDown
    private const string CollapseButtonText = "\ue70e"; // ChevronUp
    private const string ManageToolsButtonText = "\uec7a"; // DeveloperTools

    private readonly string _pinMenuItemText = CommonHelper.GetLocalizedString("PinMenuItemText");
    private readonly string _unpinMenuItemText = CommonHelper.GetLocalizedString("UnpinMenuItemRawText");
    private readonly string _unregisterMenuItemText = CommonHelper.GetLocalizedString("UnregisterMenuItemRawText");
    private readonly string _manageToolsMenuItemText = CommonHelper.GetLocalizedString("ManageExternalToolsMenuText");

    private readonly Settings _settings = Settings.Default;
    private readonly BarWindowViewModel _viewModel;
    private readonly UISettings _uiSettings = new();

    private readonly SolidColorBrush _darkModeActiveCaptionBrush;
    private readonly SolidColorBrush _darkModeDeactiveCaptionBrush;
    private readonly SolidColorBrush _nonDarkModeActiveCaptionBrush;
    private readonly SolidColorBrush _nonDarkModeDeactiveCaptionBrush;

    private bool _isClosing;
    private WindowActivationState _currentActivationState = WindowActivationState.Deactivated;

    // Constants that control window sizes
    private const int WindowPositionOffsetY = 30;
    private const int FloatingHorizontalBarHeight = 90;
    private const int FloatingHorizontalBarHeightWithExpandedCommandBar = 130;
    private const int DefaultExpandedViewTop = 30;
    private const int DefaultExpandedViewLeft = 100;
    private const int RightSideGap = 10;

    private RECT _monitorRect;

    private RestoreState _restoreState = new()
    {
        Top = DefaultExpandedViewTop,
        Left = DefaultExpandedViewLeft,
        BarOrientation = Orientation.Horizontal,
        IsLargePanelVisible = true,
    };

    private double _dpiScale = 1.0;

    internal HWND ThisHwnd { get; private set; }

    internal ClipboardMonitor? ClipboardMonitor { get; private set; }

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    public BarWindowHorizontal(BarWindowViewModel model)
    {
        _viewModel = model;

        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        InitializeComponent();
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;

        // Get the default window size. We grab this in the constructor, as
        // we may try and set our window size before our main panel gets
        // loaded (and we call SetDefaultPosition)
        var settingSize = Settings.Default.ExpandedLargeSize;
        _restoreState.Height = settingSize.Height;
        _restoreState.Width = settingSize.Width;
        ExpandCollapseLayoutButtonText.Text = _viewModel.ShowingExpandedContent ? CollapseButtonText : ExpandButtonText;

        // Precreate the brushes for the caption buttons
        // In Dark Mode, the active state is white, and the deactive state is translucent white
        // In Light Mode, the active state is black, and the deactive state is translucent black
        Windows.UI.Color color = Colors.White;
        _darkModeActiveCaptionBrush = new SolidColorBrush(color);
        color.A = 0x66;
        _darkModeDeactiveCaptionBrush = new SolidColorBrush(color);

        color = Colors.Black;
        _nonDarkModeActiveCaptionBrush = new SolidColorBrush(color);
        color.A = 0x66;
        _nonDarkModeDeactiveCaptionBrush = new SolidColorBrush(color);

        _uiSettings.ColorValuesChanged += (sender, args) =>
        {
            _dispatcher.TryEnqueue(() =>
            {
                ApplySystemThemeToCaptionButtons();
            });
        };
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

        // Apply the user's chosen theme setting.
        ThemeName t = ThemeName.Themes.First(t => t.Name == _settings.CurrentTheme);
        SetRequestedTheme(t.Theme);

        // Calculate the DPI scale.
        _dpiScale = GetDpiScaleForWindow(ThisHwnd);

        SetDefaultPosition();

        SetRegionsForTitleBar();

        PopulateCommandBar();
        ((INotifyCollectionChanged)ExternalToolsHelper.Instance.AllExternalTools).CollectionChanged += AllExternalTools_CollectionChanged;

        // Now that the position is set correctly show the window
        this.Show();
    }

    public void PopulateCommandBar()
    {
        AddManageToolsOptionToCommandBar();

        foreach (ExternalTool tool in ExternalToolsHelper.Instance.AllExternalTools)
        {
            AddToolToCommandBar(tool);
        }
    }

    private AppBarButton CreateAppBarButton(ExternalTool tool, PinOption pinOption)
    {
        AppBarButton button = new AppBarButton
        {
            Label = tool.Name,
            Tag = tool,
        };

        button.Icon = new ImageIcon
        {
            Source = tool.ToolIcon,
        };

        button.Click += _viewModel.ExternalToolButton_Click;
        button.ContextFlyout = CreateMenuFlyout(tool, pinOption);

        ToolTipService.SetToolTip(button, tool.Name);

        return button;
    }

    private MenuFlyout CreateMenuFlyout(ExternalTool tool, PinOption pinOption)
    {
        MenuFlyout menu = new MenuFlyout();
<<<<<<< HEAD
        menu.Items.Add(pinOption == PinOption.Pin ? CreatePinMenuItem(tool) : CreateUnPinMenuItem(tool));
=======
        menu.Items.Add(CreatePinMenuItem(tool, pinOption));
>>>>>>> main
        menu.Items.Add(CreateUnregisterMenuItem(tool));

        return menu;
    }

    private void AddToolToCommandBar(ExternalTool tool)
    {
        // We create 2 copies of the button, one for the primary commands list and one for the secondary commands list.
        // We're not allowed to put the same button in both lists.
        AppBarButton primaryCommandButton = CreateAppBarButton(tool, PinOption.UnPin); // The primary button should always have the unpin option
        AppBarButton secondaryCommandButton = CreateAppBarButton(tool, tool.IsPinned ? PinOption.UnPin : PinOption.Pin); // The secondary button is dynamic

        // If a tool is pinned, we'll add it to the primary commands list.
        if (tool.IsPinned)
        {
            MyCommandBar.PrimaryCommands.Add(primaryCommandButton);
        }

        // We'll always add all tools to the secondary commands list.
        MyCommandBar.SecondaryCommands.Add(secondaryCommandButton);

        tool.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(ExternalTool.ToolIcon))
            {
                // An ImageIcon can only be set once, so we can't share it with both buttons
                primaryCommandButton.Icon = new ImageIcon
                {
                    Source = tool.ToolIcon,
                };

                secondaryCommandButton.Icon = new ImageIcon
                {
                    Source = tool.ToolIcon,
                };
            }
            else if (args.PropertyName == nameof(ExternalTool.IsPinned))
            {
                // If a tool is pinned, we'll add it to the primary commands list, otherwise the secondary commands list
                secondaryCommandButton.ContextFlyout = CreateMenuFlyout(tool, tool.IsPinned ? PinOption.UnPin : PinOption.Pin);

                if (tool.IsPinned)
                {
                    MyCommandBar.PrimaryCommands.Add(primaryCommandButton);
                }
                else
                {
                    MyCommandBar.PrimaryCommands.Remove(primaryCommandButton);
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
        MyCommandBar.SecondaryCommands.Insert(0, manageToolsButton);
        MyCommandBar.SecondaryCommands.Insert(1, new AppBarSeparator());
    }

    private MenuFlyoutItem CreatePinMenuItem(ExternalTool tool, PinOption pinOption)
    {
        MenuFlyoutItem item = new MenuFlyoutItem
        {
            Text = pinOption == PinOption.Pin ? _pinMenuItemText : _unpinMenuItemText,
            Command = tool.TogglePinnedStateCommand,
            Icon = new FontIcon() { Glyph = tool.PinGlyph },
        };

        return item;
    }

    private MenuFlyoutItem CreateUnregisterMenuItem(ExternalTool tool)
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
                        AppBarButton? pinnedButton = MyCommandBar.PrimaryCommands.OfType<AppBarButton>().FirstOrDefault(b => b.Tag == oldItem);
                        if (pinnedButton is not null)
                        {
                            MyCommandBar.PrimaryCommands.Remove(pinnedButton);
                        }
                        else
                        {
                            Debug.Assert(false, "Could not find button for tool");
                        }
                    }

                    AppBarButton? button = MyCommandBar.SecondaryCommands.OfType<AppBarButton>().FirstOrDefault(b => b.Tag == oldItem);
                    if (button is not null)
                    {
                        MyCommandBar.SecondaryCommands.Remove(button);
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

    public void SetRegionsForTitleBar()
    {
        var scaleAdjustment = MainPanel.XamlRoot.RasterizationScale;

        RightPaddingColumn.Width = new GridLength(AppWindow.TitleBar.RightInset / scaleAdjustment);
        LeftPaddingColumn.Width = new GridLength(AppWindow.TitleBar.LeftInset / scaleAdjustment);

        var transform = ChromeButtonPanel.TransformToVisual(null);
        var bounds = transform.TransformBounds(new Rect(0, 0, ChromeButtonPanel.ActualWidth, ChromeButtonPanel.ActualHeight));
        Windows.Graphics.RectInt32 chromeButtonsRect = WindowHelper.GetRect(bounds, scaleAdjustment);

        var rectArray = new Windows.Graphics.RectInt32[] { chromeButtonsRect };

        InputNonClientPointerSource nonClientInputSrc =
            InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
        nonClientInputSrc.SetRegionRects(NonClientRegionKind.Passthrough, rectArray);
    }

    private void SetDefaultPosition()
    {
        // If attached to an app it should show up on the monitor that the app is on
        _monitorRect = GetMonitorRectForWindow(_viewModel.ApplicationHwnd ?? TryGetParentProcessHWND() ?? ThisHwnd);
        var screenWidth = _monitorRect.right - _monitorRect.left;
        this.Move(
            (int)((screenWidth - (Width * _dpiScale)) / 2) + _monitorRect.left,
            (int)WindowPositionOffsetY + _monitorRect.top);

        // Get the saved settings for the ExpandedView size. On first run, this will be
        // the default 0,0, so we'll set the size proportional to the monitor size.
        // Subsequently, it will be whatever size the user sets.
        var settingSize = Settings.Default.ExpandedLargeSize;
        if (settingSize.Width == 0)
        {
            settingSize.Width = (int)((_monitorRect.Width * 2) / (3 * _dpiScale));
        }

        if (settingSize.Height == 0)
        {
            settingSize.Height = (int)((_monitorRect.Height * 3) / (4 * _dpiScale));
        }

        Settings.Default.ExpandedLargeSize = settingSize;
        Settings.Default.Save();

        // Set the default restore state for the ExpandedView size to the (adjusted) settings size.
        _restoreState.Height = settingSize.Height;
        _restoreState.Width = settingSize.Width;
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

        if (LargeContentPanel is not null &&
            LargeContentPanel.Visibility == Visibility.Visible &&
            this.WindowState != WindowState.Maximized)
        {
            CacheRestoreState();
        }

        if (!_isClosing)
        {
            _isClosing = true;
            var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
            barWindow?.Close();
            _isClosing = false;
        }

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
    }

    internal void SetRequestedTheme(ElementTheme theme)
    {
        if (Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;

            if (theme == ElementTheme.Dark)
            {
                SetCaptionButtonColors(Colors.White);
            }
            else if (theme == ElementTheme.Light)
            {
                SetCaptionButtonColors(Colors.Black);
            }
            else
            {
                ApplySystemThemeToCaptionButtons();
            }
        }
    }

    private void CacheRestoreState()
    {
        _restoreState = new()
        {
            Left = AppWindow.Position.X,
            Top = AppWindow.Position.Y,
            Width = Width,
            Height = Height,
            IsLargePanelVisible = LargeContentPanel.Visibility == Visibility.Visible,
        };

        Settings.Default.ExpandedLargeSize = new System.Drawing.Size((int)Width, (int)Height);
        Settings.Default.Save();
    }

    private void ExpandLargeContentPanel()
    {
        // We're expanding.
        // Switch the bar to horizontal before we adjust the size.
        LargeContentPanel.Visibility = Visibility.Visible;
        MaxHeight = double.NaN;

        var monitorRect = GetMonitorRectForWindow(ThisHwnd);
        var dpiScale = GetDpiScaleForWindow(ThisHwnd);

        // Expand the window but keep the x,y coordinates of top-left most corner of the window the same so it doesn't
        // jump around the screen.
        var availableWidth = monitorRect.Width - Math.Abs(AppWindow.Position.X - monitorRect.left) - RightSideGap;
        _restoreState.Width = (int)((double)availableWidth / dpiScale);

        Width = _restoreState.Width;

        var availableHeight = monitorRect.Height - Math.Abs(AppWindow.Position.Y - monitorRect.top);

        _restoreState.Height = (int)((double)availableHeight / dpiScale);

        this.MoveAndResize(
            AppWindow.Position.X, AppWindow.Position.Y, _restoreState.Width, _restoreState.Height);
    }

    private void CollapseLargeContentPanel()
    {
        // Make sure we cache the state before switching to collapsed bar.
        CacheRestoreState();
        LargeContentPanel.Visibility = Visibility.Collapsed;
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

    private void MainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        SetRegionsForTitleBar();
    }

    // workaround as AppWindow TitleBar doesn't update caption button colors correctly when changed while app is running
    // https://task.ms/44172495
    public void ApplySystemThemeToCaptionButtons()
    {
        if (Content is FrameworkElement rootElement)
        {
            Windows.UI.Color color;
            if (rootElement.ActualTheme == ElementTheme.Dark)
            {
                color = Colors.White;
            }
            else
            {
                color = Colors.Black;
            }

            SetCaptionButtonColors(color);
        }

        return;
    }

    public void SetCaptionButtonColors(Windows.UI.Color color)
    {
        AppWindow.TitleBar.ButtonForegroundColor = color;
        UpdateCustomTitleBarButtonsTextColor();
    }

    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        // This follows the design guidance of dimming our title bar elements when the window isn't activated
        // https://learn.microsoft.com/en-us/windows/apps/develop/title-bar#dim-the-title-bar-when-the-window-is-inactive
        _currentActivationState = args.WindowActivationState;
        UpdateCustomTitleBarButtonsTextColor();
    }

    private void UpdateCustomTitleBarButtonsTextColor()
    {
        FrameworkElement? rootElement = Content as FrameworkElement;
        Debug.Assert(rootElement != null, "Expected Content to be a FrameworkElement");

        if (_currentActivationState == WindowActivationState.Deactivated)
        {
            SolidColorBrush brush = (rootElement.ActualTheme == ElementTheme.Dark) ? _darkModeDeactiveCaptionBrush : _nonDarkModeDeactiveCaptionBrush;

            SnapButtonText.Foreground = brush;
            ExpandCollapseLayoutButtonText.Foreground = brush;
            RotateLayoutButtonText.Foreground = brush;
        }
        else
        {
            SolidColorBrush brush = (rootElement.ActualTheme == ElementTheme.Dark) ? _darkModeActiveCaptionBrush : _nonDarkModeActiveCaptionBrush;

            SnapButtonText.Foreground = brush;
            ExpandCollapseLayoutButtonText.Foreground = brush;
            RotateLayoutButtonText.Foreground = brush;
        }
    }
}
