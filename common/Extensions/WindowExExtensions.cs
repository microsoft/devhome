// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DevHome.Common.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DevHome.Common.Extensions;

/// <summary>
/// This class add extension methods to the <see cref="Window"/> class.
/// </summary>
public static class WindowExExtensions
{
    public const int FilePickerCanceledErrorCode = unchecked((int)0x800704C7);

    /// <summary>
    /// Show an error message on the window.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="title">Dialog title.</param>
    /// <param name="content">Dialog content.</param>
    /// <param name="buttonText">Close button text.</param>
    public static async Task ShowErrorMessageDialogAsync(this Window window, string title, string content, string buttonText)
    {
        await window.ShowMessageDialogAsync(dialog =>
        {
            dialog.Title = title;
            dialog.Content = new TextBlock()
            {
                Text = content,
                TextWrapping = TextWrapping.WrapWholeWords,
            };
            dialog.PrimaryButtonText = buttonText;
        });
    }

    /// <summary>
    /// Generic implementation for creating and displaying a message dialog on
    /// a window.
    ///
    /// This extension method overloads <see cref="Window.ShowMessageDialogAsync"/>.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="action">Action performed on the created dialog.</param>
    private static async Task ShowMessageDialogAsync(this Window window, Action<ContentDialog> action)
    {
        var dialog = new ContentDialog()
        {
            XamlRoot = window.Content.XamlRoot,
        };
        action(dialog);
        await dialog.ShowAsync();
    }

    /// <summary>
    /// Set the window requested theme.
    /// </summary>
    /// <param name="window">Target window</param>
    /// <param name="theme">New theme.</param>
    public static void SetRequestedTheme(this Window window, ElementTheme theme)
    {
        if (window.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
            TitleBarHelper.UpdateTitleBar(window, rootElement.ActualTheme);
        }
    }

    /// <summary>
    /// Gets the native HWND pointer handle for the window
    /// </summary>
    /// <param name="window">The window to return the handle for</param>
    /// <returns>HWND handle</returns>
    public static IntPtr GetWindowHandle(this Microsoft.UI.Xaml.Window window)
        => window is null ? throw new ArgumentNullException(nameof(window)) : WinRT.Interop.WindowNative.GetWindowHandle(window);

    /// <summary>
    /// Centers the window on the current monitor
    /// </summary>
    /// <param name="window">The window to center</param>
    /// <param name="width">Width of the window in device independent pixels, or <c>null</c> if keeping the current size</param>
    /// <param name="height">Height of the window in device independent pixels, or <c>null</c> if keeping the current size</param>
    public static void CenterOnScreen(this Microsoft.UI.Xaml.Window window, double? width = null, double? height = null)
    {
        var hwnd = window.GetWindowHandle();
        var hwndDesktop = PInvoke.MonitorFromWindow((HWND)hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        var info = default(MONITORINFO);
        info.cbSize = (uint)Marshal.SizeOf(info);
        PInvoke.GetMonitorInfo(hwndDesktop, ref info);
        var dpi = PInvoke.GetDpiForWindow((HWND)hwnd);
        PInvoke.GetWindowRect((HWND)hwnd, out RECT windowRect);
        var scalingFactor = dpi / 96d;
        var w = width.HasValue ? (int)(width * scalingFactor) : windowRect.right - windowRect.left;
        var h = height.HasValue ? (int)(height * scalingFactor) : windowRect.bottom - windowRect.top;
        var cx = (info.rcMonitor.left + info.rcMonitor.right) / 2;
        var cy = (info.rcMonitor.bottom + info.rcMonitor.top) / 2;
        var left = cx - (w / 2);
        var top = cy - (h / 2);
        PInvoke.SetWindowPos((HWND)hwnd, (HWND)IntPtr.Zero, left, top, w, h, SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
    }
}
