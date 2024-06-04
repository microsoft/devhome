// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using DevHome.Common.Extensions;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using DevHome.PI.Properties;
using DevHome.PI.ViewModels;
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

public partial class BarWindowVertical : WindowEx
{
    private readonly Settings _settings = Settings.Default;
    private readonly string _errorTitleText = CommonHelper.GetLocalizedString("ToolLaunchErrorTitle");
    private readonly string _errorMessageText = CommonHelper.GetLocalizedString("ToolLaunchErrorMessage");
    private readonly BarWindowViewModel _viewModel;

    private int _cursorPosX; // = 0;
    private int _cursorPosY; // = 0;
    private int _appWindowPosX; // = 0;
    private int _appWindowPosY; // = 0;
    private bool isWindowMoving; // = false;
    private bool isClosing;
    private const int UnsnapGap = 9;

    private readonly WINEVENTPROC _winPositionEventDelegate;
    private readonly WINEVENTPROC _winFocusEventDelegate;

    private Button? _selectedExternalToolButton;

    private HWINEVENTHOOK _positionEventHook;
    private HWINEVENTHOOK _focusEventHook;

    internal HWND ThisHwnd { get; private set; }

    public Microsoft.UI.Dispatching.DispatcherQueue TheDispatcher
    {
        get; set;
    }

    public BarWindowVertical(BarWindowViewModel model)
    {
        _viewModel = model;

        // The main constructor is used in all cases, including when there's no target window.
        TheDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        InitializeComponent();
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        _winPositionEventDelegate = new(WinPositionEventProc);
        _winFocusEventDelegate = new(WinFocusEventProc);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BarWindowViewModel.IsSnapped))
        {
            if (_viewModel.IsSnapped)
            {
                Snap();
            }
            else
            {
                Unsnap();
            }
        }
    }

    private void MainPanel_Loaded(object sender, RoutedEventArgs e)
    {
        ThisHwnd = (HWND)WindowNative.GetWindowHandle(this);

        // Apply the user's chosen theme setting.
        ThemeName t = ThemeName.Themes.First(t => t.Name == _settings.CurrentTheme);
        SetRequestedTheme(t.Theme);

        // Regardless of what is set in the XAML, our initial window width is too big. Setting this to 70 (same as the XAML file)
        Width = 70;
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
                        string.Format(CultureInfo.CurrentCulture, _errorMessageText, tool.Executable),
                        _errorTitleText,
                        MESSAGEBOX_STYLE.MB_ICONERROR);
                }
            }
        }
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        if (_positionEventHook != IntPtr.Zero)
        {
            PInvoke.UnhookWinEvent(_positionEventHook);
            _positionEventHook = HWINEVENTHOOK.Null;
        }

        if (_focusEventHook != HWINEVENTHOOK.Null)
        {
            PInvoke.UnhookWinEvent(_focusEventHook);
            _focusEventHook = HWINEVENTHOOK.Null;
        }

        if (!isClosing)
        {
            isClosing = true;
            var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
            barWindow?.Close();
            isClosing = false;
        }
    }

    private void ExternalToolButton_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _selectedExternalToolButton = (Button)sender;
    }

    private void UnPinMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // TODO Implement unpinning a tool from the bar, assuming we continue with the pinning feature.
    }

    private void UnregisterMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedExternalToolButton is not null)
        {
            if (_selectedExternalToolButton.Tag is ExternalTool tool)
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
                if (_viewModel.IsSnapped)
                {
                    // If the window has been maximized, un-snap the bar window and free-float it.
                    if (PInvoke.IsZoomed(TargetAppData.Instance.HWnd))
                    {
                        _viewModel.IsSnapped = false;
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

            // If the window we're watching closes, we unsnap
            if (eventType == PInvoke.EVENT_OBJECT_DESTROY)
            {
                Unsnap();
            }
        }
    }

    private void WinFocusEventProc(HWINEVENTHOOK hWinEventHook, uint eventType, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
    {
        // If we're snapped to a target window, and that window loses and then regains focus,
        // we need to bring our window to the front also, to be in-sync. Otherwise, we can
        // end up with the target in the foreground, but our window partially obscured.
        if (hwnd == TargetAppData.Instance.HWnd && _viewModel.IsSnapped)
        {
            this.SetIsAlwaysOnTop(true);
            this.SetIsAlwaysOnTop(false);
            return;
        }
    }

    private void Snap()
    {
        Debug.Assert(_positionEventHook == HWINEVENTHOOK.Null, "Hook should be cleared");
        Debug.Assert(_focusEventHook == HWINEVENTHOOK.Null, "Hook should be cleared");

        _positionEventHook = WatchWindowPositionEvents(_winPositionEventDelegate, (uint)TargetAppData.Instance.ProcessId);
        _focusEventHook = WatchWindowFocusEvents(_winFocusEventDelegate, (uint)TargetAppData.Instance.ProcessId);

        SnapToWindow();
    }

    private void Unsnap()
    {
        // Set a gap from the associated app window to provide positive feedback.
        this.MoveAndResize(
            AppWindow.Position.X + UnsnapGap,
            AppWindow.Position.Y,
            AppWindow.Size.Width,
            AppWindow.Size.Height);

        if (_positionEventHook != HWINEVENTHOOK.Null)
        {
            PInvoke.UnhookWinEvent(_positionEventHook);
            _positionEventHook = HWINEVENTHOOK.Null;
        }

        if (_focusEventHook != HWINEVENTHOOK.Null)
        {
            PInvoke.UnhookWinEvent(_focusEventHook);
            _focusEventHook = HWINEVENTHOOK.Null;
        }
    }

    private void SnapToWindow()
    {
        Debug.Assert(_viewModel.IsSnapped, "We're not snapped!");

        WindowHelper.SnapToWindow(TargetAppData.Instance.HWnd, ThisHwnd, AppWindow.Size);

        this.SetIsAlwaysOnTop(true);
        this.SetIsAlwaysOnTop(false);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        var primaryWindow = Application.Current.GetService<PrimaryWindow>();
        Close();
        primaryWindow.ClearBarWindow();
    }

    private void Window_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        ((UIElement)sender).ReleasePointerCaptures();
        isWindowMoving = false;

        // If we're occupying the same space as the target window, and we're not in medium/large mode, snap to the app
        if (!_viewModel.IsSnapped && TargetAppData.Instance.HWnd != HWND.Null)
        {
            if (DoesWindow1CoverTheRightSideOfWindow2(ThisHwnd, TargetAppData.Instance.HWnd))
            {
                _viewModel.IsSnapped = true;
            }
        }
    }

    private void Window_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint((UIElement)sender).Properties;
        if (properties.IsLeftButtonPressed)
        {
            // Moving the window causes it to unsnap
            if (_viewModel.IsSnapped)
            {
                _viewModel.IsSnapped = false;
            }

            isWindowMoving = true;
            ((UIElement)sender).CapturePointer(e.Pointer);
            _appWindowPosX = AppWindow.Position.X;
            _appWindowPosY = AppWindow.Position.Y;
            PInvoke.GetCursorPos(out var pt);
            _cursorPosX = pt.X;
            _cursorPosY = pt.Y;
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
                    _appWindowPosX + (pt.X - _cursorPosX), _appWindowPosY + (pt.Y - _cursorPosY)));
            }

            e.Handled = true;
        }
    }
}
