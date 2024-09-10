// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32.SafeHandles;
using Serilog;
using Windows.Devices.Display;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.SystemInformation;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.Shell.Common;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DevHome.DevDiagnostics.Helpers;

public class WindowHelper
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WindowHelper));

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

    public static RectInt32 GetRect(Rect bounds, double scale)
    {
        return new RectInt32(
            _X: (int)Math.Round(bounds.X * scale),
            _Y: (int)Math.Round(bounds.Y * scale),
            _Width: (int)Math.Round(bounds.Width * scale),
            _Height: (int)Math.Round(bounds.Height * scale));
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

    public static async Task<SoftwareBitmapSource> GetWinUI3BitmapSourceFromGdiBitmap(Bitmap bmp)
    {
        var softwareBitmap = GetSoftwareBitmapFromGdiBitmap(bmp);
        var source = new SoftwareBitmapSource();
        await source.SetBitmapAsync(softwareBitmap);
        return source;
    }

    public static SoftwareBitmap? GetSoftwareBitmapFromExecutable(string executable)
    {
        SoftwareBitmap? softwareBitmap = null;
        var toolIcon = Icon.ExtractAssociatedIcon(executable);

        // Fall back to Windows default app icon.
        toolIcon ??= Icon.FromHandle(LoadDefaultAppIcon());

        if (toolIcon is not null)
        {
            softwareBitmap = GetSoftwareBitmapFromGdiBitmap(toolIcon.ToBitmap());
        }

        return softwareBitmap;
    }

    public static SoftwareBitmap GetSoftwareBitmapFromGdiBitmap(Bitmap bmp)
    {
        // Get pixels as an array of bytes.
        var data = bmp.LockBits(
            new Rectangle(0, 0, bmp.Width, bmp.Height),
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            bmp.PixelFormat);
        var bytes = new byte[data.Stride * data.Height];
        Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
        bmp.UnlockBits(data);

        // Get WinRT SoftwareBitmap.
        var softwareBitmap = new SoftwareBitmap(
            BitmapPixelFormat.Bgra8,
            bmp.Width,
            bmp.Height,
            BitmapAlphaMode.Premultiplied);
        softwareBitmap.CopyFromBuffer(bytes.AsBuffer());

        return softwareBitmap;
    }

    public static async Task<SoftwareBitmapSource> GetSoftwareBitmapSourceFromSoftwareBitmapAsync(SoftwareBitmap softwareBitmap)
    {
        var softwareBitmapSource = new SoftwareBitmapSource();
        await softwareBitmapSource.SetBitmapAsync(softwareBitmap);
        return softwareBitmapSource;
    }

    internal static BitmapImage? GetBitmapImageFromFile(string filePath)
    {
        BitmapImage? bitmapImage = null;
        try
        {
            // Creating a BitmapImage must be done on the UI thread.
            bitmapImage = new BitmapImage();
            var icon = Icon.ExtractAssociatedIcon(filePath);
            if (icon is not null)
            {
                using var bitmap = icon.ToBitmap();
                using var stream = new MemoryStream();
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

                stream.Seek(0, SeekOrigin.Begin);
                var randomAccessStream = new InMemoryRandomAccessStream();
                var outputStream = randomAccessStream.GetOutputStreamAt(0);
                var writer = new DataWriter(outputStream);
                writer.WriteBytes(stream.ToArray());
                writer.StoreAsync().GetResults();
                randomAccessStream.Seek(0);

                bitmapImage.SetSource(randomAccessStream);
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Error creating BitmapImage from filePath: {ex.Message}");
        }

        return bitmapImage;
    }

    internal static async Task<SoftwareBitmapSource?> GetSoftwareBitmapSourceFromImageFilePath(string iconFilePath)
    {
        SoftwareBitmapSource? softwareBitmapSource = null;

        try
        {
            var fileStream = File.OpenRead(iconFilePath);
            var decoder = await BitmapDecoder.CreateAsync(fileStream.AsRandomAccessStream());
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            // SoftwareBitmapSource.SetBitmapAsync only supports SoftwareBitmap with
            // bgra8 pixel format and pre-multiplied or no alpha.
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8
                || softwareBitmap.BitmapAlphaMode == BitmapAlphaMode.Straight)
            {
                softwareBitmap = SoftwareBitmap.Convert(
                    softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }

            softwareBitmapSource = new SoftwareBitmapSource();
            await softwareBitmapSource.SetBitmapAsync(softwareBitmap);
        }
        catch (Exception ex)
        {
            _log.Error($"Error creating SoftwareBitmapSource from file path: {ex.Message}");
        }

        return softwareBitmapSource;
    }

    internal static unsafe uint GetProcessIdFromWindow(HWND hWnd)
    {
        uint processID = 0;
        _ = PInvoke.GetWindowThreadProcessId(hWnd, &processID);
        return processID;
    }

    internal static void TranslateUWPProcess(HWND hWnd, ref Process process)
    {
        if (process.ProcessName.Equals("ApplicationFrameHost", StringComparison.OrdinalIgnoreCase))
        {
            var processId = GetProcessIdFromUWPWindow(hWnd);
            if (processId != 0)
            {
                process = Process.GetProcessById((int)processId);
            }
        }
    }

    internal static unsafe uint GetProcessIdFromUWPWindow(HWND hWnd)
    {
        UWPProcessFinder processFinder = new();
        PInvoke.EnumChildWindows(hWnd, processFinder.EnumChildWindowsProc, IntPtr.Zero);
        return processFinder.UWPProcessId;
    }

    private sealed class UWPProcessFinder
    {
        public uint UWPProcessId { get; private set; }

        public unsafe BOOL EnumChildWindowsProc(HWND hWnd, LPARAM data)
        {
            var className = stackalloc char[256];
            var classNameLength = PInvoke.GetClassName(hWnd, className, 256);
            if (classNameLength > 0)
            {
                string classNameString = new(className, 0, classNameLength);
                if (classNameString.StartsWith("Windows.UI.Core.", StringComparison.OrdinalIgnoreCase))
                {
                    uint hWndProcessId = 0;
                    _ = PInvoke.GetWindowThreadProcessId(hWnd, &hWndProcessId);
                    UWPProcessId = hWndProcessId;
                    return false;
                }
            }

            return true;
        }
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

    private static RECT GetWorkAreaRect()
    {
        RECT rect = default;
        unsafe
        {
            PInvoke.SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETWORKAREA, 0, &rect, 0);
        }

        return rect;
    }

    internal static RECT GetMonitorRectForWindow(HWND hWnd)
    {
        var monitor = PInvoke.MonitorFromWindow(hWnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        var monitorInfo = new MONITORINFO { cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO)) };
        PInvoke.GetMonitorInfo(monitor, ref monitorInfo);
        var screenBounds = monitorInfo.rcMonitor;
        return screenBounds;
    }

    internal static double GetDpiScaleForWindow(HWND hWnd)
    {
        var monitor = PInvoke.MonitorFromWindow(hWnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        PInvoke.GetScaleFactorForMonitor(monitor, out DEVICE_SCALE_FACTOR scaleFactor).ThrowOnFailure();
        return (double)scaleFactor / 100;
    }

    internal static void GetAppInfoUnderMouseCursor(out Process? process, out HWND hwnd)
    {
        process = null;

        // Grab the window under the cursor and attach to that process
        PInvoke.GetCursorPos(out var pt);
        hwnd = PInvoke.WindowFromPoint(pt);

        if (hwnd != HWND.Null)
        {
            // Walk up until we get the topmost parent window
            HWND hwndParent = PInvoke.GetParent(hwnd);

            while (hwndParent != HWND.Null)
            {
                hwnd = hwndParent;
                hwndParent = PInvoke.GetParent(hwnd);
            }

            var processID = WindowHelper.GetProcessIdFromWindow(hwnd);

            if (processID != 0)
            {
                process = Process.GetProcessById((int)processID);
            }
        }
    }

    // Only one ContentDialog can be shown at a time, so we have to keep track of the current one.
    private static ContentDialog? ContentDialog { get; set; }

    internal static async void ShowTimedMessageDialog(FrameworkElement frameworkElement, string message, string closeButtonText)
    {
        if (ContentDialog is not null)
        {
            ContentDialog.Hide();
            ContentDialog = null;
        }

        ContentDialog = new ContentDialog
        {
            XamlRoot = frameworkElement.XamlRoot,
            RequestedTheme = frameworkElement.ActualTheme,
            Content = message,
            CloseButtonText = closeButtonText,
        };

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3),
        };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            if (ContentDialog is null)
            {
                return;
            }

            ContentDialog.Hide();
            ContentDialog = null;
        };

        try
        {
            ContentDialog.Opened += (s, e) => timer.Start();
            ContentDialog.Closed += (s, e) =>
            {
                timer.Stop();
                ContentDialog = null;
            };
            await ContentDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error showing timed message dialog");
        }
    }

    internal enum CpuArchitecture
    {
        X86,
        X64,
        ARM,
        ARM64,
        Unknown,
    }

    internal static CpuArchitecture GetTargetArchitecture(IMAGE_FILE_MACHINE target)
    {
        return target switch
        {
            IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_I386 => CpuArchitecture.X86,
            IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_AMD64 => CpuArchitecture.X64,
            IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARM => CpuArchitecture.ARM,
            IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARM64 => CpuArchitecture.ARM64,
            _ => CpuArchitecture.Unknown,
        };
    }

    internal static string GetAppArchitecture(SafeProcessHandle handle, string moduleName)
    {
        var cpuArchitecture = CommonHelper.GetLocalizedString("CpuArchitecture_Unknown");

        try
        {
            unsafe
            {
                IMAGE_FILE_MACHINE processInfo;
                IMAGE_FILE_MACHINE machineInfo;
                var isWow64Result = PInvoke.IsWow64Process2(handle, out processInfo, &machineInfo);
                if (isWow64Result)
                {
                    var processArchitecture = GetTargetArchitecture(processInfo);
                    var machineArchitecture = GetTargetArchitecture(machineInfo);

                    // "Unknown" means this is not a WOW64 process.
                    if (processArchitecture == CpuArchitecture.Unknown)
                    {
                        if (machineArchitecture == CpuArchitecture.X64)
                        {
                            // If this is an x64 machine and it's not a WOW64 process, it's an x64 process.
                            cpuArchitecture = CommonHelper.GetLocalizedString("CpuArchitecture_X64onX64");
                        }
                        else
                        {
                            // If this is not an x64 machine, we need to get the process architecture from the process itself.
                            var processMachineInfo = default(PROCESS_MACHINE_INFORMATION);
                            var getProcInfoResult
                                = PInvoke.GetProcessInformation(
                                handle,
                                PROCESS_INFORMATION_CLASS.ProcessMachineTypeInfo,
                                &processMachineInfo,
                                (uint)Marshal.SizeOf<PROCESS_MACHINE_INFORMATION>());
                            if (getProcInfoResult)
                            {
                                // Report the process architecture and the machine architecture.
                                processArchitecture = GetTargetArchitecture(processMachineInfo.ProcessMachine);
                                cpuArchitecture = CommonHelper.GetLocalizedString(
                                    "CpuArchitecture_ProcessOnMachine", processArchitecture, machineArchitecture);
                            }
                            else
                            {
                                // If we can't get the process architecture, just report the machine architecture.
                                cpuArchitecture = CommonHelper.GetLocalizedString(
                                    "CpuArchitecture_UnknownOnMachine", machineArchitecture);
                            }
                        }
                    }
                    else
                    {
                        // This is a WOW64 process, so report the process architecture and the machine architecture.
                        cpuArchitecture = CommonHelper.GetLocalizedString(
                            "CpuArchitecture_ProcessOnMachine", processArchitecture, machineArchitecture);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error getting app architecture");
        }

        return cpuArchitecture;
    }
}
