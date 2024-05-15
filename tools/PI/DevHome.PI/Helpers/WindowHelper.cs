// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Devices.Display;
using Windows.Devices.Enumeration;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DevHome.PI.Helpers;

public class WindowHelper
{
    private static nint GetClassLongPtr(HWND hWnd, GET_CLASS_LONG_INDEX nIndex)
    {
        if (IntPtr.Size == 8)
        {
            return CommonInterop.GetClassLongPtr64(hWnd, nIndex);
        }
        else
        {
            return (nint)PInvoke.GetClassLong(hWnd, nIndex);
        }
    }

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

    private static nint GetWindowLongPtr(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex)
    {
        if (IntPtr.Size == 8)
        {
            return CommonInterop.GetWindowLongPtr64(hWnd, nIndex);
        }
        else
        {
            return PInvoke.GetWindowLong(hWnd, nIndex);
        }
    }

    // TODO The SnapOffsetHorizontal and SnapThreshold values don't allow for different DPIs.

    // It seems the way rounded corners are implemented means that the window is really 8px
    // bigger than it seems, so we'll subtract this when we do sidecar snapping.
    private const int SnapOffsetHorizontal = 8;

    // If the target window is moved to within SnapThreshold px of the edge of the screen, we unsnap.
    private const int SnapThreshold = 10;

    private static unsafe BOOL EnumProc(HWND hWnd, LPARAM data)
    {
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        var enumData = (EnumWindowsData*)data.Value;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

        // The caller should've set this, but we'll make sure here.
        enumData->OutHwnd = HWND.Null;

        // Skip this one if the window doesn't include WS_VISIBLE, or if it's minimized.
        if (!PInvoke.IsWindowVisible(hWnd))
        {
            return true;
        }

        if (PInvoke.IsIconic(hWnd))
        {
            return true;
        }

        // Skip toolwindows.
        var extendedStyle = GetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        var isToolWindow = (extendedStyle & (long)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW)
            == (long)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW;
        if (isToolWindow)
        {
            return true;
        }

        // Skip dialogs.
        if (PInvoke.GetAncestor(hWnd, GET_ANCESTOR_FLAGS.GA_ROOTOWNER) != hWnd)
        {
            return true;
        }

        PInvoke.GetWindowRect(hWnd, out var windowRect);
        var screenBounds = GetMonitorRectForWindow(hWnd);
        var isOnAnyScreen =
            windowRect.left < screenBounds.right && windowRect.right > screenBounds.left &&
            windowRect.top < screenBounds.bottom && windowRect.bottom > screenBounds.top &&
            windowRect.right - windowRect.left > 1 && windowRect.bottom - windowRect.top > 1;
        if (!isOnAnyScreen)
        {
            return true;
        }

        unsafe
        {
            // Exclude system/shell windows.
            var className = stackalloc char[256];
            var classNameLength = PInvoke.GetClassName(hWnd, className, 256);
            if (classNameLength == 0)
            {
                return true;
            }

            string classNameString = new(className, 0, classNameLength);
            if (classNameString == "Progman" || classNameString == "Shell_TrayWnd" ||
                classNameString == "WorkerW" || classNameString == "SHELLDLL_DefView" ||
                classNameString == "IME")
            {
                return true;
            }

            // Exclude cloaked windows.
            var cloakedVal = 0;
            var hRes = PInvoke.DwmGetWindowAttribute(
                hWnd, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, &cloakedVal, sizeof(int));
            if (hRes != 0)
            {
                cloakedVal = 0;
            }

            if (cloakedVal != 0)
            {
                return true;
            }

            // Skip any windows that are on our exclusion list.
            var excludedProcesses = enumData->ExcludedProcesses;
            if (excludedProcesses != null && !IsAcceptableWindow(hWnd, excludedProcesses))
            {
                return true;
            }
        }

        // Skip popups, unless they're for UWP apps or for dialog-based MFC apps.
        var windowStyle = (WINDOW_STYLE)GetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        var isPopup = (windowStyle & WINDOW_STYLE.WS_POPUP) == WINDOW_STYLE.WS_POPUP;

        // Dialog-based MFC apps will have WS_POPUP but also WS_OVERLAPPED.
        var isOverlapped = (windowStyle & WINDOW_STYLE.WS_OVERLAPPED) == WINDOW_STYLE.WS_OVERLAPPED;

        if (isPopup)
        {
            var isUwpApp = IsProcessName(hWnd, "applicationframehost");
            if (isUwpApp)
            {
                // NOTE: We could use SHGetPropertyStoreForWindow + PKEY_AppUserModel_ID
                // to get the appid for the app.
                // Found a visible UWP window, stop enumerating.
                enumData->OutHwnd = hWnd;
                return false;
            }
            else if (isOverlapped)
            {
                // This is a top-level popup, most likely a dialog-based MFC app.
                enumData->OutHwnd = hWnd;
                return false;
            }

            return true;
        }

        // Found a window, stop enumerating.
        enumData->OutHwnd = hWnd;
        return false;
    }

    private static Rectangle GetScreenBounds()
    {
        Rectangle rectangle = default;

        // Can't use async in EnumProc.
        var deviceInfoOp = DeviceInformation.FindAllAsync(DisplayMonitor.GetDeviceSelector());
        deviceInfoOp.AsTask().Wait();
        var displayList = deviceInfoOp.GetResults();
        if (displayList == null || displayList.Count == 0)
        {
            return rectangle;
        }

        var winSize = default(SizeInt32);
        var displayOp = DisplayMonitor.FromInterfaceIdAsync(displayList[0].Id);
        displayOp.AsTask().Wait();
        var monitorInfo = displayOp.GetResults();
        if (monitorInfo == null)
        {
            winSize.Width = 800;
            winSize.Height = 1200;
        }
        else
        {
            winSize.Height = monitorInfo.NativeResolutionInRawPixels.Height;
            winSize.Width = monitorInfo.NativeResolutionInRawPixels.Width;
        }

        rectangle.Width = winSize.Width;
        rectangle.Height = winSize.Height;

        return rectangle;
    }

    public enum BinaryType : int
    {
        Unknown = -1,
        X32 = 0,
        X64 = 6,
    }

    internal static unsafe string GetWindowTitle(HWND hWnd)
    {
        var length = PInvoke.GetWindowTextLength(hWnd);
        var windowText = stackalloc char[length];
        _ = PInvoke.GetWindowText(hWnd, windowText, length);
        return new string(windowText);
    }

    internal static IntPtr LoadDefaultAppIcon()
    {
        IntPtr icon = PInvoke.LoadIcon(HINSTANCE.Null, PInvoke.IDI_APPLICATION);
        return icon;
    }

    internal static Bitmap? GetAppIcon(HWND hWnd)
    {
        try
        {
            // Try getting the big icon first.
            IntPtr hIcon = default;
            hIcon = PInvoke.SendMessage(hWnd, PInvoke.WM_GETICON, PInvoke.ICON_BIG, IntPtr.Zero);

            // If that failed, try getting the small icon (or the system-provided default).
            if (hIcon == IntPtr.Zero)
            {
                hIcon = PInvoke.SendMessage(hWnd, PInvoke.WM_GETICON, PInvoke.ICON_SMALL2, IntPtr.Zero);
                if (hIcon == IntPtr.Zero)
                {
                    hIcon = (nint)GetClassLongPtr(hWnd, GET_CLASS_LONG_INDEX.GCL_HICON);
                }
            }

            if (hIcon != IntPtr.Zero)
            {
                return new Bitmap(Icon.FromHandle(hIcon).ToBitmap(), 24, 24);
            }
            else
            {
                return null;
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static async Task<SoftwareBitmapSource> GetWinUI3BitmapSourceFromGdiBitmap(System.Drawing.Bitmap bmp)
    {
        // get pixels as an array of bytes
        var data = bmp.LockBits(
            new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            bmp.PixelFormat);
        var bytes = new byte[data.Stride * data.Height];
        Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
        bmp.UnlockBits(data);

        // get WinRT SoftwareBitmap
        var softwareBitmap = new Windows.Graphics.Imaging.SoftwareBitmap(
            Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
            bmp.Width,
            bmp.Height,
            Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
        softwareBitmap.CopyFromBuffer(bytes.AsBuffer());

        // build WinUI3 SoftwareBitmapSource
        var source = new SoftwareBitmapSource();
        await source.SetBitmapAsync(softwareBitmap);
        return source;
    }

    internal static unsafe uint GetProcessIdFromWindow(HWND hWnd)
    {
        uint processID = 0;
        _ = PInvoke.GetWindowThreadProcessId(hWnd, &processID);
        return processID;
    }

    internal static HWINEVENTHOOK WatchWindowPositionEvents(WINEVENTPROC procDelegate, uint processID)
    {
        var eventHook = PInvoke.SetWinEventHook(
            PInvoke.EVENT_OBJECT_DESTROY,
            PInvoke.EVENT_OBJECT_LOCATIONCHANGE,
            HMODULE.Null,
            procDelegate,
            processID,
            0,
            PInvoke.WINEVENT_OUTOFCONTEXT | PInvoke.WINEVENT_SKIPOWNPROCESS);
        return eventHook;
    }

    internal static HWINEVENTHOOK WatchWindowForegroundEvents(WINEVENTPROC procDelegate)
    {
        var eventHook = PInvoke.SetWinEventHook(
            PInvoke.EVENT_SYSTEM_FOREGROUND,
            PInvoke.EVENT_SYSTEM_FOREGROUND,
            HMODULE.Null,
            procDelegate,
            0,
            0,
            PInvoke.WINEVENT_OUTOFCONTEXT | PInvoke.WINEVENT_SKIPOWNPROCESS);
        return eventHook;
    }

    internal static HWINEVENTHOOK WatchWindowFocusEvents(WINEVENTPROC procDelegate, uint processID)
    {
        var eventHook = PInvoke.SetWinEventHook(
            PInvoke.EVENT_OBJECT_FOCUS,
            PInvoke.EVENT_OBJECT_FOCUS,
            HMODULE.Null,
            procDelegate,
            processID,
            0,
            PInvoke.WINEVENT_OUTOFCONTEXT | PInvoke.WINEVENT_SKIPOWNPROCESS);
        return eventHook;
    }

    internal sealed class EnumWindowsData
    {
        public HWND OutHwnd { get; set; }

        public StringCollection? ExcludedProcesses { get; set; }

        public EnumWindowsData()
        {
            OutHwnd = HWND.Null;
        }
    }

    internal static unsafe HWND FindVisibleForegroundWindow(StringCollection excludedProcesses)
    {
        EnumWindowsData data = new() { ExcludedProcesses = excludedProcesses };
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        LPARAM lparamData = new((nint)(&data));
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        PInvoke.EnumWindows(EnumProc, lparamData);
        return data.OutHwnd;
    }

    internal static bool IsAcceptableWindow(HWND hWnd, StringCollection excludedProcesses)
    {
        if (excludedProcesses != null && excludedProcesses.Count > 0)
        {
            foreach (var processName in excludedProcesses)
            {
                if (processName != null && IsProcessName(hWnd, processName))
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal static unsafe bool IsProcessName(HWND hWnd, string name)
    {
        uint processId = 0;
        _ = PInvoke.GetWindowThreadProcessId(hWnd, &processId);
        var process = Process.GetProcessById((int)processId);
        if (string.Equals(process.ProcessName, name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    internal static string GetProcessName(uint processId)
    {
        var process = Process.GetProcessById((int)processId);
        return process.ProcessName;
    }

    internal static void SetWindowExTransparent(HWND hwnd)
    {
        var extendedStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        _ = PInvoke.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)(extendedStyle | WINDOW_EX_STYLE.WS_EX_TRANSPARENT));
    }

    internal static void SetWindowExNotTransparent(HWND hwnd)
    {
        var extendedStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        _ = PInvoke.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)(extendedStyle & ~WINDOW_EX_STYLE.WS_EX_TRANSPARENT));
    }

    internal static T? FindParentControl<T>(DependencyObject child)
        where T : DependencyObject
    {
        var parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null)
        {
            return null;
        }

        if (parentObject is T parent)
        {
            return parent;
        }
        else
        {
            return FindParentControl<T>(parentObject);
        }
    }

    internal static void SnapToWindow(IntPtr targetHwnd, IntPtr dbarHwnd, SizeInt32 size)
    {
        PInvoke.GetWindowRect((HWND)targetHwnd, out var rect);
        PInvoke.MoveWindow((HWND)dbarHwnd, rect.right - SnapOffsetHorizontal, rect.top, size.Width, size.Height, true);
    }

    internal static bool IsWindowSnapped(HWND hwnd)
    {
        if (!PInvoke.GetWindowRect(hwnd, out var windowRect))
        {
            return false;
        }

        var workAreaRect = GetWorkAreaRect();

        // If the window is within the top, right or bottom (not left) snap threshold,
        // consider it snapped to the edge.
        var snappedToTop = Math.Abs(windowRect.top - workAreaRect.top) <= SnapThreshold;
        var snappedToRight = Math.Abs(windowRect.right - workAreaRect.right) <= SnapThreshold;
        var snappedToBottom = Math.Abs(windowRect.bottom - workAreaRect.bottom) <= SnapThreshold;
        return snappedToTop || snappedToRight || snappedToBottom;
    }

    internal static bool DoWindowsOverlap(HWND hwnd, HWND hwnd2)
    {
        PInvoke.GetWindowRect(hwnd, out var rect);
        PInvoke.GetWindowRect(hwnd2, out var rect2);

        var overlap = rect.left < rect2.right && rect.right > rect2.left &&
                      rect.top < rect2.bottom && rect.bottom > rect2.top;

        return overlap;
    }

    internal static bool DoesWindow1CoverTheRightSideOfWindow2(HWND hwnd1, HWND hwnd2)
    {
        PInvoke.GetWindowRect(hwnd1, out var rect);
        PInvoke.GetWindowRect(hwnd2, out var rect2);

        // We'll consider the right side of the window being the far right quarter of the window. Adjust the window's rect to match what we want
        rect2.left = rect2.right - ((rect2.right - rect2.left) / 4);

        var overlap = rect.left < rect2.right && rect.right > rect2.left &&
                      rect.top < rect2.bottom && rect.bottom > rect2.top;

        return overlap;
    }

    private static RECT GetWorkAreaRect()
    {
        RECT rect = default;
        unsafe
        {
            PInvoke.SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETWORKAREA, 0, &rect, 0);
        }

        return rect;
    }

    // TODO Allow for the taskbar when returning screen size.
    internal static RECT GetMonitorRectForWindow(HWND hWnd)
    {
        var monitor = PInvoke.MonitorFromWindow(hWnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        var monitorInfo = new MONITORINFO { cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO)) };
        PInvoke.GetMonitorInfo(monitor, ref monitorInfo);
        var screenBounds = monitorInfo.rcMonitor;
        return screenBounds;
    }

    internal static void GetAppInfoUnderMouseCursor(out Process? process, out HWND hwnd)
    {
        process = null;

        // Grab the window under the cursor and attach to that process
        PInvoke.GetCursorPos(out var pt);
        hwnd = PInvoke.WindowFromPoint(pt);

        if (hwnd != HWND.Null)
        {
            var processID = WindowHelper.GetProcessIdFromWindow(hwnd);

            if (processID != 0)
            {
                process = Process.GetProcessById((int)processID);
            }
        }
    }
}
