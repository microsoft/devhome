// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.PI.Models;
using DevHome.PI.ViewModels;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using WinUIEx;

namespace DevHome.PI.Helpers;

public class SnapHelper
{
    // TODO The SnapOffsetHorizontal and UnsnapGap values don't allow for different DPIs.
    private const int UnsnapGap = 9;

    // It seems the way rounded corners are implemented means that the window is really 8px
    // bigger than it seems, so we'll subtract this when we do sidecar snapping.
    private const int SnapOffsetHorizontal = 8;

    private readonly WINEVENTPROC _winPositionEventDelegate;
    private readonly WINEVENTPROC _winFocusEventDelegate;

    private HWINEVENTHOOK _positionEventHook;
    private HWINEVENTHOOK _focusEventHook;

    public SnapHelper()
    {
        _winPositionEventDelegate = new(WinPositionEventProc);
        _winFocusEventDelegate = new(WinFocusEventProc);
    }

    public void Snap()
    {
        Debug.Assert(_positionEventHook == HWINEVENTHOOK.Null, "Hook should be cleared");
        Debug.Assert(_focusEventHook == HWINEVENTHOOK.Null, "Hook should be cleared");

        _positionEventHook = WindowHelper.WatchWindowPositionEvents(_winPositionEventDelegate, (uint)TargetAppData.Instance.ProcessId);
        _focusEventHook = WindowHelper.WatchWindowFocusEvents(_winFocusEventDelegate, (uint)TargetAppData.Instance.ProcessId);

        SnapToWindow();
    }

    public void Unsnap()
    {
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        Debug.Assert(barWindow != null, "BarWindow should not be null.");

        // Set a gap from the associated app window to provide positive feedback.
        PInvoke.GetWindowRect(barWindow.CurrentHwnd, out var rect);
        barWindow.UpdateBarWindowPosition(new PointInt32(rect.left + UnsnapGap, rect.top));

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
                var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
                Debug.Assert(barWindow != null, "BarWindow should not be null.");
                if (barWindow.IsBarSnappedToWindow())
                {
                    // If the window has been maximized, un-snap the bar window and free-float it.
                    if (PInvoke.IsZoomed(TargetAppData.Instance.HWnd))
                    {
                        barWindow.UnsnapBarWindow();
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
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        Debug.Assert(barWindow != null, "BarWindow should not be null.");
        if (hwnd == TargetAppData.Instance.HWnd && barWindow.IsBarSnappedToWindow())
        {
            barWindow.ResetBarWindowVisibility();
            return;
        }
    }

    private void SnapToWindow()
    {
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        Debug.Assert(barWindow != null, "BarWindow should not be null.");
        Debug.Assert(barWindow.IsBarSnappedToWindow(), "We're not snapped!");

        if (barWindow.CurrentHwnd == PInvoke.GetForegroundWindow())
        {
            PInvoke.SetForegroundWindow(TargetAppData.Instance.HWnd);
        }

        PInvoke.GetWindowRect(TargetAppData.Instance.HWnd, out var rect);
        barWindow.UpdateBarWindowPosition(new PointInt32(rect.right - SnapOffsetHorizontal, rect.top));
        barWindow.ResetBarWindowVisibility();
    }
}
