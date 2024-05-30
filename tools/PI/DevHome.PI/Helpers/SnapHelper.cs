// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using DevHome.PI.Models;
using DevHome.PI.ViewModels;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using WinUIEx;

namespace DevHome.PI.Helpers;

public class SnapHelper
{
    private const int UnsnapGap = 9;

    private readonly WINEVENTPROC _winPositionEventDelegate;
    private readonly WINEVENTPROC _winFocusEventDelegate;
    private readonly BarWindowViewModel _viewModel;
    private readonly BarWindowVertical _barWindowVertical;

    private HWINEVENTHOOK _positionEventHook;
    private HWINEVENTHOOK _focusEventHook;

    public SnapHelper(BarWindowViewModel viewModel, BarWindowVertical barWindowVertical)
    {
        _viewModel = viewModel;
        _barWindowVertical = barWindowVertical;
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
            _viewModel.IsAlwaysOnTop = true;
            _viewModel.IsAlwaysOnTop = false;
            return;
        }
    }

    private void Snap()
    {
        Debug.Assert(_positionEventHook == HWINEVENTHOOK.Null, "Hook should be cleared");
        Debug.Assert(_focusEventHook == HWINEVENTHOOK.Null, "Hook should be cleared");

        _positionEventHook = WindowHelper.WatchWindowPositionEvents(_winPositionEventDelegate, (uint)TargetAppData.Instance.ProcessId);
        _focusEventHook = WindowHelper.WatchWindowFocusEvents(_winFocusEventDelegate, (uint)TargetAppData.Instance.ProcessId);

        SnapToWindow();
    }

    private void Unsnap()
    {
        // Set a gap from the associated app window to provide positive feedback.
        var appWindow = _barWindowVertical.AppWindow;
        _barWindowVertical.MoveAndResize(appWindow.Position.X + UnsnapGap, appWindow.Position.Y, appWindow.Size.Width, appWindow.Size.Height);

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

        WindowHelper.SnapToWindow(TargetAppData.Instance.HWnd, _barWindowVertical.ThisHwnd, _barWindowVertical.AppWindow.Size);

        _viewModel.IsAlwaysOnTop = true;
        _viewModel.IsAlwaysOnTop = false;
    }

    public void Close()
    {
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
}
