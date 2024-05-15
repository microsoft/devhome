// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using DevHome.Common.Extensions;
using DevHome.PI.Controls;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using DevHome.PI.Properties;
using DevHome.PI.Telemetry;
using DevHome.PI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.UI.WindowManagement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;
using WinUIEx;
using static DevHome.PI.Helpers.WindowHelper;

namespace DevHome.PI;

public partial class BarWindow : WindowEx, INotifyPropertyChanged
{
    private readonly Settings settings = Settings.Default;
    private readonly string errorTitleText = CommonHelper.GetLocalizedString("ToolLaunchErrorTitle");
    private readonly string errorMessageText = CommonHelper.GetLocalizedString("ToolLaunchErrorMessage");
    private readonly BarWindowViewModel viewModel = new();

    // Constants that control window sizes
    private const int WindowPositionOffsetY = 30;
    private const int FloatingHorizontalBarWidth = 700;
    private const int FloatingHorizontalBarHeight = 70;
    private const int FloatingVerticalBarWidth = 70;
    private const int FloatingVerticalBarHeight = 700;
    private const int DefaultExpandedViewTop = 30;
    private const int DefaultExpandedViewLeft = 100;
    private const int RightSideGap = 10;

    private readonly GridLength _gridLengthStar = new(1, GridUnitType.Star);
    private int cursorPosX; // = 0;
    private int cursorPosY; // = 0;
    private int appWindowPosX; // = 0;
    private int appWindowPosY; // = 0;
    private bool isWindowMoving; // = false;

    private Orientation _barOrientation = Orientation.Horizontal;

    public Orientation BarOrientation
    {
        get => _barOrientation;
        set
        {
            _barOrientation = value;

            if (value == Orientation.Horizontal)
            {
                SBarHorizontal.Visibility = Visibility.Visible;
                SBarVertical.Visibility = Visibility.Collapsed;
                ExternalToolsRepeater.Layout = Application.Current.Resources["ExternalToolsHorizontalLayout"] as StackLayout;
                MainPanelMiddleRowDefinition.Height = GridLength.Auto;
                MainPanelLastRowDefinition.Height = _gridLengthStar;
                SystemResourceStackPanel.SetValue(Grid.RowProperty, 0);
                SystemResourceStackPanel.SetValue(Grid.ColumnProperty, 2);
                TopGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
            }
            else
            {
                SBarHorizontal.Visibility = Visibility.Collapsed;
                SBarVertical.Visibility = Visibility.Visible;
                ExternalToolsRepeater.Layout = Application.Current.Resources["ExternalToolsVerticalLayout"] as StackLayout;
                MainPanelMiddleRowDefinition.Height = _gridLengthStar;
                MainPanelLastRowDefinition.Height = GridLength.Auto;
                SystemResourceStackPanel.SetValue(Grid.RowProperty, 2);
                SystemResourceStackPanel.SetValue(Grid.ColumnProperty, 0);
                TopGrid.HorizontalAlignment = HorizontalAlignment.Center;
            }

            OnPropertyChanged(nameof(BarOrientation));
        }
    }

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

    private bool _isSnapped;

    private bool IsSnapped
    {
        get => _isSnapped;
        set
        {
            _isSnapped = value;
            SBarHorizontal.IsSnapped = value;
            SBarVertical.IsSnapped = value;
        }
    }

    private bool _isMaximized;

    private bool IsMaximized
    {
        get => _isMaximized;
        set
        {
            _isMaximized = value;
            SBarHorizontal.IsMaximized = value;
            SBarVertical.IsMaximized = value;

            if (value)
            {
                WindowState = WindowState.Maximized;
            }
        }
    }

    private readonly ObservableCollection<Button> externalTools = [];

    private readonly WINEVENTPROC winPositionEventDelegate;
    private readonly WINEVENTPROC winFocusEventDelegate;

    private Button? selectedExternalToolButton;

    private HWINEVENTHOOK positionEventHook;
    private HWINEVENTHOOK focusEventHook;

    internal static HWND ThisHwnd { get; private set; }

    internal ClipboardMonitor? ClipboardMonitor { get; private set; }

    public List<Window> OpenChildWindows { get; private set; } = [];

    public bool ShouldExpandLargeWindow { get; set; }

    public Microsoft.UI.Dispatching.DispatcherQueue TheDispatcher
    {
        get; set;
    }

    public BarWindow(Process targetProcess, IntPtr hWnd)
        : this()
    {
        // This constructor is used when we're starting the app with a target window.
        TargetAppData.Instance.SetNewAppData(targetProcess, (HWND)hWnd);
    }

    public BarWindow()
    {
        // The main constructor is used in all cases, including when there's no target window.
        TheDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        InitializeComponent();

        var presenter = (OverlappedPresenter)AppWindow.Presenter;
        presenter.IsResizable = false;
        presenter.SetBorderAndTitleBar(false, false);

        SBarHorizontal.Initialize();
        SBarVertical.Initialize();
        AppStatus.Initialize(this);
        TelemetryReporter.SetWindow(this);
        winPositionEventDelegate = new(WinPositionEventProc);
        winFocusEventDelegate = new(WinFocusEventProc);

        // Enable the user to drag/move the window.
        Content.PointerMoved += Window_PointerMoved;
        Content.PointerPressed += Window_PointerPressed;
        Content.PointerReleased += Window_PointerReleased;

        ShouldExpandLargeWindow = false;
    }

    private void MainPanel_Loaded(object sender, RoutedEventArgs e)
    {
        ThisHwnd = (HWND)WindowNative.GetWindowHandle(this);

        settings.PropertyChanged += Settings_PropertyChanged;

        if (settings.IsClipboardMonitoringEnabled)
        {
            ClipboardMonitor.Instance.Start(ThisHwnd);
        }

        if (settings.IsCpuUsageMonitoringEnabled)
        {
            PerfCounters.Instance.Start();
        }

        ExternalToolsHelper.Instance.Init();

        // Apply the user's chosen theme setting.
        ThemeName t = ThemeName.Themes.First(t => t.Name == settings.CurrentTheme);
        SetRequestedTheme(t.Theme);

        // Calculate the DPI scale. We'll also recalculate later,
        // in case the user changes the display settings.
        var dpiWindow = HwndExtensions.GetDpiForWindow(ThisHwnd);
        dpiScale = dpiWindow / 96.0;

        SwitchBarToHorizontal();
        SetDefaultPosition();

        if (ShouldExpandLargeWindow)
        {
            ExpandCollapseLargeContentPanel();
        }

        // Show the window after it has been positioned and configured.
        this.Show();
    }

    private void SetDefaultPosition()
    {
        Debug.Assert(!IsSnapped, "We should not be snapped when setting the default position.");

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
        TargetAppData.Instance.ClearAppData();

        if (positionEventHook != IntPtr.Zero)
        {
            PInvoke.UnhookWinEvent(positionEventHook);
            positionEventHook = HWINEVENTHOOK.Null;
        }

        if (focusEventHook != HWINEVENTHOOK.Null)
        {
            PInvoke.UnhookWinEvent(focusEventHook);
            focusEventHook = HWINEVENTHOOK.Null;
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

    private void Window_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        ((UIElement)sender).ReleasePointerCaptures();
        isWindowMoving = false;

        // If we're occupying the same space as the target window, and we're not in medium/large mode, snap to the app
        if (!IsSnapped && TargetAppData.Instance.HWnd != HWND.Null && LargeContentPanel.Visibility == Visibility.Collapsed)
        {
            if (DoesWindow1CoverTheRightSideOfWindow2(ThisHwnd, TargetAppData.Instance.HWnd))
            {
                Snap();
            }
        }
    }

    private void Window_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint((UIElement)sender).Properties;
        if (properties.IsLeftButtonPressed)
        {
            // Moving the window causes it to unsnap
            if (IsSnapped)
            {
                Unsnap();
            }

            isWindowMoving = true;
            ((UIElement)sender).CapturePointer(e.Pointer);
            appWindowPosX = AppWindow.Position.X;
            appWindowPosY = AppWindow.Position.Y;
            PInvoke.GetCursorPos(out var pt);
            cursorPosX = pt.X;
            cursorPosY = pt.Y;
        }
    }

    private void Window_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (isWindowMoving)
        {
            var properties = e.GetCurrentPoint((UIElement)sender).Properties;
            if (properties.IsLeftButtonPressed)
            {
                PInvoke.GetCursorPos(out var pt);
                AppWindow.Move(new Windows.Graphics.PointInt32(
                    appWindowPosX + (pt.X - cursorPosX), appWindowPosY + (pt.Y - cursorPosY)));
            }

            e.Handled = true;
        }
    }

    private void WinPositionEventProc(HWINEVENTHOOK hWinEventHook, uint eventType, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
    {
        // Filter out events for non-main windows.
        if (idObject != 0 || idChild != 0)
        {
            return;
        }

        if (hwnd == TargetAppData.Instance.HWnd)
        {
            if (eventType == PInvoke.EVENT_OBJECT_LOCATIONCHANGE)
            {
                if (IsSnapped)
                {
                    // If the window has been maximized, un-snap the bar window and free-float it.
                    if (PInvoke.IsZoomed(TargetAppData.Instance.HWnd))
                    {
                        Unsnap();
                    }
                    else
                    {
                        // Reposition the window to match the moved/resized/minimized/restored target window.
                        // If the target window was maximized and has now been restored, we want
                        // to resnap to it, but not do all the other work we do when we resnap
                        // to a new window.
                        SnapToWindow();
                    }
                }
            }

            // NOTE We no longer close the window when the window we're snapped to closes.
            // Instead we just stop watching the window and look for a new foreground window.
        }

        if (eventType == PInvoke.EVENT_OBJECT_DESTROY)
        {
            Unsnap();
        }
    }

    private void WinFocusEventProc(HWINEVENTHOOK hWinEventHook, uint eventType, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
    {
        // If we're snapped to a target window, and that window loses and then regains focus,
        // we need to bring our window to the front also, to be in-sync. Otherwise, we can
        // end up with the target in the foreground, but our window partially obscured.
        if (hwnd == TargetAppData.Instance.HWnd && IsSnapped)
        {
            this.SetIsAlwaysOnTop(true);
            this.SetIsAlwaysOnTop(false);
            return;
        }
    }

    public void HandleMinimizeRequest()
    {
        CacheRestoreState();
        WindowState = WindowState.Minimized;
    }

    public void HandleMaximizeRequest()
    {
        Debug.Assert(WindowState != WindowState.Maximized, "We should not be maximizing when already maximized.");

        CacheRestoreState();
        if (BarOrientation == Orientation.Vertical)
        {
            SwitchBarToHorizontal();
        }

        LargeContentPanel.Visibility = Visibility.Visible;
        var presenter = (OverlappedPresenter)AppWindow.Presenter;
        presenter.IsResizable = false;

        IsMaximized = true;

        LargeContentButton.IsEnabled = false;
    }

    private void CacheRestoreState()
    {
        restoreState = new()
        {
            Left = AppWindow.Position.X,
            Top = AppWindow.Position.Y,
            Width = Width,
            Height = Height,
            BarOrientation = BarOrientation,
            IsLargePanelVisible = LargeContentPanel.Visibility == Visibility.Visible,
        };

        Settings.Default.ExpandedLargeSize = new System.Drawing.Size((int)Width, (int)Height);
        Settings.Default.Save();
    }

    public void HandleRestoreRequest()
    {
        WindowState = WindowState.Normal;
        Height = restoreState.Height;
        Width = restoreState.Width;

        if (restoreState.BarOrientation == Orientation.Vertical)
        {
            SwitchBarToVertical();
        }

        if (restoreState.IsLargePanelVisible)
        {
            LargeContentPanel.Visibility = Visibility.Visible;
        }

        AppWindow.Move(new Windows.Graphics.PointInt32((int)restoreState.Left, (int)restoreState.Top));

        IsMaximized = false;
        LargeContentButton.IsEnabled = true;
    }

    public void HandleCloseRequest()
    {
        var primaryWindow = Application.Current.GetService<PrimaryWindow>();
        primaryWindow.ClearBarWindow();
        Close();
    }

    public void HandleCloseAllRequest()
    {
        var primaryWindow = Application.Current.GetService<PrimaryWindow>();
        primaryWindow.ClearBarWindow();
        Close();
        primaryWindow.Close();
    }

    private void SwitchLayoutButton_Click(object sender, RoutedEventArgs e)
    {
        SwitchLayout();
    }

    private void SwitchLayout()
    {
        var presenter = (OverlappedPresenter)AppWindow.Presenter;
        presenter.IsResizable = false;
        LargeContentButton.IsEnabled = true;

        if (BarOrientation == Orientation.Horizontal)
        {
            SwitchBarToVertical();
        }
        else
        {
            SwitchBarToHorizontal();
        }
    }

    private void SwitchBarToVertical()
    {
        // We only allow sizing when the LargeContentPanel is visible.
        if (LargeContentPanel.Visibility == Visibility.Visible)
        {
            CacheRestoreState();
            var presenter = (OverlappedPresenter)AppWindow.Presenter;
            presenter.IsResizable = false;
            LargeContentPanel.Visibility = Visibility.Collapsed;
        }

        MainCommandGridView.HorizontalAlignment = HorizontalAlignment.Center;
        BarOrientation = Orientation.Vertical;

        Width = FloatingVerticalBarWidth;
        Height = FloatingVerticalBarHeight;
    }

    private void SwitchBarToHorizontal()
    {
        MainCommandGridView.HorizontalAlignment = HorizontalAlignment.Left;
        BarOrientation = Orientation.Horizontal;

        Width = FloatingHorizontalBarWidth;
        Height = FloatingHorizontalBarHeight;
    }

    private void LargeContentButton_Click(object sender, RoutedEventArgs e)
    {
        ExpandCollapseLargeContentPanel();
    }

    private void ExpandCollapseLargeContentPanel()
    {
        IsMaximized = false;
        var presenter = (OverlappedPresenter)AppWindow.Presenter;

        if (LargeContentPanel.Visibility == Visibility.Collapsed)
        {
            // We're expanding.
            // Switch the bar to horizontal before we adjust the size.
            SwitchBarToHorizontal();
            LargeContentPanel.Visibility = Visibility.Visible;
            presenter.IsResizable = true;

            // If they expand to ExpandedView and they're not snapped, we can use the
            // RestoreState size & position.
            if (!IsSnapped)
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
        else
        {
            // We're collapsing.
            // Make sure we cache the state before switching to collapsed bar.
            CacheRestoreState();
            SwitchBarToHorizontal();
            LargeContentPanel.Visibility = Visibility.Collapsed;
            presenter.IsResizable = false;
            LargeContentButton.IsEnabled = true;
        }
    }

    private void Snap()
    {
        Debug.Assert(!IsSnapped, "Snapping when we're already snapped");
        Debug.Assert(positionEventHook == HWINEVENTHOOK.Null, "Hook should be cleared");
        Debug.Assert(focusEventHook == HWINEVENTHOOK.Null, "Hook should be cleared");

        positionEventHook = WatchWindowPositionEvents(winPositionEventDelegate, (uint)TargetAppData.Instance.ProcessId);
        focusEventHook = WatchWindowFocusEvents(winFocusEventDelegate, (uint)TargetAppData.Instance.ProcessId);

        IsSnapped = true;
        SwitchBarToVertical();

        SnapToWindow();
    }

    private void Unsnap()
    {
        Debug.Assert(IsSnapped, "We should be snapped");
        IsSnapped = false;

        // Set a gap from the associated app window to provide positive feedback.
        this.MoveAndResize(
            (AppWindow.Position.X + UnsnapGap) * dpiScale,
            AppWindow.Position.Y * dpiScale,
            AppWindow.Size.Width * dpiScale,
            AppWindow.Size.Height * dpiScale);

        if (positionEventHook != HWINEVENTHOOK.Null)
        {
            PInvoke.UnhookWinEvent(positionEventHook);
            positionEventHook = HWINEVENTHOOK.Null;
        }

        if (focusEventHook != HWINEVENTHOOK.Null)
        {
            PInvoke.UnhookWinEvent(focusEventHook);
            focusEventHook = HWINEVENTHOOK.Null;
        }
    }

    private void SnapToWindow()
    {
        Debug.Assert(IsSnapped, "We're not snapped!");

        WindowHelper.SnapToWindow(TargetAppData.Instance.HWnd, ThisHwnd, AppWindow.Size);

        this.SetIsAlwaysOnTop(true);
        this.SetIsAlwaysOnTop(false);
    }

    public void PerformSnapAction()
    {
        if (IsSnapped)
        {
            Unsnap();
        }
        else
        {
            Snap();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void ProcessChooserButton_Click(object sender, RoutedEventArgs e)
    {
        ExpandedViewControl.NavigateTo(typeof(ProcessListPageViewModel));
        ExpandCollapseLargeContentPanel();
    }

    internal void NavigateTo(Type viewModelType)
    {
        ExpandedViewControl.NavigateTo(viewModelType);
    }

    internal Frame GetFrame()
    {
        return ExpandedViewControl.GetPageFrame();
    }
}
