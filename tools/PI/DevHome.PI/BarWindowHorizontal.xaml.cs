// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using DevHome.Common.Extensions;
using DevHome.PI.Controls;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using DevHome.PI.Properties;
using DevHome.PI.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.WindowManagement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Shell.Common;
using WinRT.Interop;
using WinUIEx;
using static DevHome.PI.Helpers.WindowHelper;

namespace DevHome.PI;

public partial class BarWindowHorizontal : WindowEx
{
    private readonly Settings _settings = Settings.Default;
    private readonly BarWindowViewModel _viewModel;
    private bool isClosing;

    // Constants that control window sizes
    private const int _WindowPositionOffsetY = 30;
    private const int _FloatingHorizontalBarHeight = 70;
    private const int _DefaultExpandedViewTop = 30;
    private const int _DefaultExpandedViewLeft = 100;
    private const int _RightSideGap = 10;

    private RECT _monitorRect;

    private RestoreState _restoreState = new()
    {
        Top = _DefaultExpandedViewTop,
        Left = _DefaultExpandedViewLeft,
        BarOrientation = Orientation.Horizontal,
        IsLargePanelVisible = true,
    };

    private const int _UnsnapGap = 9;
    private double _dpiScale = 1.0;

    internal HWND ThisHwnd { get; private set; }

    internal ClipboardMonitor? ClipboardMonitor { get; private set; }

    public Microsoft.UI.Dispatching.DispatcherQueue TheDispatcher
    {
        get; set;
    }

    public BarWindowHorizontal(BarWindowViewModel model)
    {
        _viewModel = model;

        TheDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

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
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BarWindowViewModel.ShowingExpandedContent))
        {
            if (_viewModel.ShowingExpandedContent)
            {
                ExpandLargeContentPanel();
            }
            else
            {
                CollapseLargeContentPanel();
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
        var monitor = PInvoke.MonitorFromWindow(ThisHwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        PInvoke.GetScaleFactorForMonitor(monitor, out DEVICE_SCALE_FACTOR scaleFactor).ThrowOnFailure();
        _dpiScale = (double)scaleFactor / 100;

        SetDefaultPosition();

        SetRegionsForTitleBar();
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
        _monitorRect = GetMonitorRectForWindow(_viewModel.ApplicationHwnd ?? ThisHwnd);
        var screenWidth = _monitorRect.right - _monitorRect.left;
        this.Move(
            (int)((screenWidth - (Width * _dpiScale)) / 2) + _monitorRect.left,
            (int)_WindowPositionOffsetY);

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

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        ClipboardMonitor.Instance.Stop();

        if (LargeContentPanel is not null &&
            LargeContentPanel.Visibility == Visibility.Visible &&
            this.WindowState != WindowState.Maximized)
        {
            CacheRestoreState();
        }

        if (!isClosing)
        {
            isClosing = true;
            var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
            barWindow?.Close();
            isClosing = false;
        }
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

        // If they expand to ExpandedView and they're not snapped, we can use the
        // RestoreState size & position.
        if (!_viewModel.IsSnapped)
        {
            this.MoveAndResize(
                _restoreState.Left, _restoreState.Top, _restoreState.Width, _restoreState.Height);
        }
        else
        {
            // Conversely if they're snapped, the position is determined by the snap,
            // and we potentially adjust the size to ensure it doesn't extend beyond the screen.
            var availableWidth = _monitorRect.Width - Math.Abs(AppWindow.Position.X) - _RightSideGap;
            if (availableWidth < _restoreState.Width)
            {
                _restoreState.Width = availableWidth;
            }

            Width = _restoreState.Width;

            var availableHeight = _monitorRect.Height - Math.Abs(AppWindow.Position.Y);
            if (availableHeight < _restoreState.Height)
            {
                _restoreState.Height = availableHeight;
            }

            Height = _restoreState.Height;
        }
    }

    private void CollapseLargeContentPanel()
    {
        // Make sure we cache the state before switching to collapsed bar.
        CacheRestoreState();
        LargeContentPanel.Visibility = Visibility.Collapsed;
        MaxHeight = _FloatingHorizontalBarHeight;
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
}
