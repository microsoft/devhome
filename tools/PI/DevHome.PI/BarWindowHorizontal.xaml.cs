// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using DevHome.PI.Controls;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using DevHome.PI.Properties;
using DevHome.PI.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI.WindowManagement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;
using WinUIEx;
using static DevHome.PI.Helpers.WindowHelper;

namespace DevHome.PI;

public partial class BarWindowHorizontal : WindowEx
{
    private readonly Settings settings = Settings.Default;
    private readonly string errorTitleText = CommonHelper.GetLocalizedString("ToolLaunchErrorTitle");
    private readonly string errorMessageText = CommonHelper.GetLocalizedString("ToolLaunchErrorMessage");
    private readonly BarWindowViewModel viewModel;
    private readonly ObservableCollection<Button> externalTools = [];

    private Button? selectedExternalToolButton;

    // Constants that control window sizes
    private const int WindowPositionOffsetY = 30;
    private const int FloatingHorizontalBarHeight = 70;
    private const int DefaultExpandedViewTop = 30;
    private const int DefaultExpandedViewLeft = 100;
    private const int RightSideGap = 10;

    private RECT monitorRect;

    private RestoreState restoreState = new()
    {
        Top = DefaultExpandedViewTop,
        Left = DefaultExpandedViewLeft,
        BarOrientation = Orientation.Horizontal,
        IsLargePanelVisible = true,
    };

    private const int UnsnapGap = 9;
    private double dpiScale = 1.0;

    internal HWND ThisHwnd { get; private set; }

    internal ClipboardMonitor? ClipboardMonitor { get; private set; }

    public Microsoft.UI.Dispatching.DispatcherQueue TheDispatcher
    {
        get; set;
    }

    public BarWindowHorizontal(BarWindowViewModel model)
    {
        viewModel = model;

        // The main constructor is used in all cases, including when there's no target window.
        TheDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        InitializeComponent();
        viewModel.PropertyChanged += ViewModel_PropertyChanged;

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;

        // Get the default window size. We grab this in the constructor, as
        // we may try and set our window size before our main panel gets
        // loaded (and we call SetDefaultPosition)
        var settingSize = Settings.Default.ExpandedLargeSize;
        restoreState.Height = settingSize.Height;
        restoreState.Width = settingSize.Width;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BarWindowViewModel.ShowingExpandedContent))
        {
            if (viewModel.ShowingExpandedContent)
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

        settings.PropertyChanged += Settings_PropertyChanged;

        if (settings.IsClipboardMonitoringEnabled)
        {
            ClipboardMonitor.Instance.Start(ThisHwnd);
        }

        ExternalToolsHelper.Instance.Init();

        // Apply the user's chosen theme setting.
        ThemeName t = ThemeName.Themes.First(t => t.Name == settings.CurrentTheme);
        SetRequestedTheme(t.Theme);

        // Calculate the DPI scale. We'll also recalculate later,
        // in case the user changes the display settings.
        var dpiWindow = HwndExtensions.GetDpiForWindow(ThisHwnd);
        dpiScale = dpiWindow / 96.0;

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
        Windows.Graphics.RectInt32 chromeButtonsRect = GetRect(bounds, scaleAdjustment);

        var rectArray = new Windows.Graphics.RectInt32[] { chromeButtonsRect };

        InputNonClientPointerSource nonClientInputSrc =
            InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
        nonClientInputSrc.SetRegionRects(NonClientRegionKind.Passthrough, rectArray);
    }

    protected Windows.Graphics.RectInt32 GetRect(Rect bounds, double scale)
    {
        return new Windows.Graphics.RectInt32(
            _X: (int)Math.Round(bounds.X * scale),
            _Y: (int)Math.Round(bounds.Y * scale),
            _Width: (int)Math.Round(bounds.Width * scale),
            _Height: (int)Math.Round(bounds.Height * scale));
    }

    private void SetDefaultPosition()
    {
        monitorRect = GetMonitorRectForWindow(ThisHwnd);
        var screenWidth = monitorRect.right - monitorRect.left;
        this.Move(
            (int)(((screenWidth - Width) / 2) * dpiScale),
            (int)(WindowPositionOffsetY * dpiScale));

        // Get the saved settings for the ExpandedView size. On first run, this will be
        // the default 0,0, so we'll set the size proportional to the monitor size.
        // Subsequently, it will be whatever size the user sets.
        var settingSize = Settings.Default.ExpandedLargeSize;
        if (settingSize.Width == 0)
        {
            settingSize.Width = monitorRect.Width * 2 / 3;
        }

        if (settingSize.Height == 0)
        {
            settingSize.Height = monitorRect.Height * 3 / 4;
        }

        Settings.Default.ExpandedLargeSize = settingSize;
        Settings.Default.Save();

        // Set the default restore state for the ExpandedView size to the (adjusted) settings size.
        restoreState.Height = settingSize.Height;
        restoreState.Width = settingSize.Width;
    }

    private void ExternalToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button clickedButton)
        {
            if (clickedButton.Tag is ExternalTool tool)
            {
                var process = tool.Invoke(TargetAppData.Instance.TargetProcess?.Id, TargetAppData.Instance.HWnd);

                if (process == null)
                {
                    // It appears ContentDialogs only render in the space it's parent occupies. Since the parent is a narrow
                    // bar, the dialog doesn't have enough space to render. So, we'll use MessageBox to display errors.
                    PInvoke.MessageBox(
                        ThisHwnd,
                        string.Format(CultureInfo.CurrentCulture, errorMessageText, tool.Executable),
                        errorTitleText,
                        MESSAGEBOX_STYLE.MB_ICONERROR);
                }
            }
        }
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
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.IsClipboardMonitoringEnabled))
        {
            if (settings.IsClipboardMonitoringEnabled)
            {
                ClipboardMonitor.Instance.Start(ThisHwnd);
            }
            else
            {
                ClipboardMonitor.Instance.Stop();
            }
        }
    }

    private void ExternalToolButton_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        selectedExternalToolButton = (Button)sender;
    }

    private void UnPinMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // TODO Implement unpinning a tool from the bar, assuming we continue with the pinning feature.
    }

    private void UnregisterMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (selectedExternalToolButton is not null)
        {
            if (selectedExternalToolButton.Tag is ExternalTool tool)
            {
                ExternalToolsHelper.Instance.RemoveExternalTool(tool);
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
        restoreState = new()
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
        if (!viewModel.IsSnapped)
        {
            this.MoveAndResize(
                restoreState.Left, restoreState.Top, restoreState.Width, restoreState.Height);
        }
        else
        {
            // Conversely if they're snapped, the position is determined by the snap,
            // and we potentially adjust the size to ensure it doesn't extend beyond the screen.
            var availableWidth = monitorRect.Width - Math.Abs(AppWindow.Position.X) - RightSideGap;
            if (availableWidth < restoreState.Width)
            {
                restoreState.Width = availableWidth;
            }

            Width = restoreState.Width;

            var availableHeight = monitorRect.Height - Math.Abs(AppWindow.Position.Y);
            if (availableHeight < restoreState.Height)
            {
                restoreState.Height = availableHeight;
            }

            Height = restoreState.Height;
        }
    }

    private void CollapseLargeContentPanel()
    {
        // Make sure we cache the state before switching to collapsed bar.
        CacheRestoreState();
        LargeContentPanel.Visibility = Visibility.Collapsed;
        MaxHeight = FloatingHorizontalBarHeight;
    }

    private void ProcessChooserButton_Click(object sender, RoutedEventArgs e)
    {
        viewModel.ShowingExpandedContent = true;
        ExpandedViewControl.NavigateTo(typeof(ProcessListPageViewModel));
    }

    internal void NavigateTo(Type viewModelType)
    {
        viewModel.ShowingExpandedContent = true;
        ExpandedViewControl.NavigateTo(viewModelType);
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
