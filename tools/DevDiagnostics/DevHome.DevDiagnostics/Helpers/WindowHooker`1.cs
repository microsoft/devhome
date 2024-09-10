// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Serilog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DevHome.DevDiagnostics.Helpers;

internal abstract class WindowHooker<T>
{
    private static nint SetWindowLongPtr(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, nint newLong)
    {
        if (IntPtr.Size == 8)
        {
            return CommonInterop.SetWindowLongPtr64(hWnd, nIndex, newLong);
        }
        else
        {
            return (nint)PInvoke.SetWindowLong(hWnd, nIndex, (int)newLong);
        }
    }

    protected static readonly ILogger Log = Serilog.Log.ForContext("SourceContext", nameof(T));

    private readonly WNDPROC windowProcHook;

    private HWND listenerHwnd;

    private WNDPROC? originalWndProc;

    protected HWND ListenerHwnd { get => listenerHwnd; set => listenerHwnd = value; }

    internal WindowHooker()
    {
        windowProcHook = CustomWndProc;
    }

    public virtual void Start(HWND listeningWindow)
    {
        if (ListenerHwnd != HWND.Null)
        {
            // No-OP if we're already running
            Debug.Assert(ListenerHwnd == listeningWindow, "Why are we trying to start with a different hwnd?");
            return;
        }

        ArgumentNullException.ThrowIfNull(listeningWindow, nameof(listeningWindow));

        var wndproc = SetWindowLongPtr(listeningWindow, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate<WNDPROC>(windowProcHook));
        if (wndproc == IntPtr.Zero)
        {
            Log.Error("SetWindowLongPtr failed: {GetLastError}", Marshal.GetLastWin32Error().ToString(CultureInfo.InvariantCulture));
            return;
        }

        originalWndProc = Marshal.GetDelegateForFunctionPointer<WNDPROC>(wndproc);
        ListenerHwnd = listeningWindow;
    }

    public virtual void Stop()
    {
        if (ListenerHwnd != HWND.Null)
        {
            Debug.Assert(originalWndProc != null, "Where did the original wndproc go?");

            var result = SetWindowLongPtr(ListenerHwnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate<WNDPROC>(originalWndProc));
            if (result == IntPtr.Zero)
            {
                Log.Error("SetWindowLongPtr failed: {GetLastError}", Marshal.GetLastWin32Error().ToString(CultureInfo.InvariantCulture));
            }

            ListenerHwnd = HWND.Null;
        }
    }

    protected virtual LRESULT CustomWndProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        return PInvoke.CallWindowProc(originalWndProc, hWnd, msg, wParam, lParam);
    }
}
