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
    private readonly Settings settings = Settings.Default;
    private readonly string errorTitleText = CommonHelper.GetLocalizedString("ToolLaunchErrorTitle");
    private readonly string errorMessageText = CommonHelper.GetLocalizedString("ToolLaunchErrorMessage");
    private readonly BarWindowViewModel viewModel;

    private int cursorPosX; // = 0;
    private int cursorPosY; // = 0;
    private int appWindowPosX; // = 0;
    private int appWindowPosY; // = 0;
    private bool isWindowMoving; // = false;
    private const int UnsnapGap = 9;

    private readonly WINEVENTPROC winPositionEventDelegate;
    private readonly WINEVENTPROC winFocusEventDelegate;

    private Button? selectedExternalToolButton;

    private HWINEVENTHOOK positionEventHook;
    private HWINEVENTHOOK focusEventHook;

    internal HWND ThisHwnd { get; private set; }

    public Microsoft.UI.Dispatching.DispatcherQueue TheDispatcher
    {
        get; set;
    }

    public BarWindowVertical(BarWindowViewModel model)
    {
        viewModel = model;

        // The main constructor is used in all cases, including when there's no target window.
        TheDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        InitializeComponent();
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        winPositionEventDelegate = new(WinPositionEventProc);
        winFocusEventDelegate = new(WinFocusEventProc);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BarWindowViewModel.IsSnapped))
        {
            if (viewModel.IsSnapped)
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
        ThemeName t = ThemeName.Themes.First(t => t.Name == settings.CurrentTheme);
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
                        string.Format(CultureInfo.CurrentCulture, errorMessageText, tool.Executable),
                        errorTitleText,
                        MESSAGEBOX_STYLE.MB_ICONERROR);
                }
            }
        }
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
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
                if (viewModel.IsSnapped)
                {
                    // If the window has been maximized, un-snap the bar window and free-float it.
                    if (PInvoke.IsZoomed(TargetAppData.Instance.HWnd))
                    {
                        viewModel.IsSnapped = false;
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
        if (hwnd == TargetAppData.Instance.HWnd && viewModel.IsSnapped)
        {
            this.SetIsAlwaysOnTop(true);
            this.SetIsAlwaysOnTop(false);
            return;
        }
    }

    private void Snap()
    {
        Debug.Assert(positionEventHook == HWINEVENTHOOK.Null, "Hook should be cleared");
        Debug.Assert(focusEventHook == HWINEVENTHOOK.Null, "Hook should be cleared");

        positionEventHook = WatchWindowPositionEvents(winPositionEventDelegate, (uint)TargetAppData.Instance.ProcessId);
        focusEventHook = WatchWindowFocusEvents(winFocusEventDelegate, (uint)TargetAppData.Instance.ProcessId);

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
        Debug.Assert(viewModel.IsSnapped, "We're not snapped!");

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
        if (!viewModel.IsSnapped && TargetAppData.Instance.HWnd != HWND.Null)
        {
            if (DoesWindow1CoverTheRightSideOfWindow2(ThisHwnd, TargetAppData.Instance.HWnd))
            {
                viewModel.IsSnapped = true;
            }
        }
    }

    private void Window_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint((UIElement)sender).Properties;
        if (properties.IsLeftButtonPressed)
        {
            // Moving the window causes it to unsnap
            if (viewModel.IsSnapped)
            {
                viewModel.IsSnapped = false;
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
}
