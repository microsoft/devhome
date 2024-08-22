// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DevHome.DevInsights.Helpers;

internal sealed class CommonInterop
{
    // CSWin32 will not produce these methods for x86 so we need to define them here.
    [DllImport("user32.dll", ExactSpelling = true, EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    internal static extern nint SetWindowLongPtr64(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, nint dwNewLong);

    [DllImport("user32.dll", ExactSpelling = true, EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    internal static extern nint GetWindowLongPtr64(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex);

    [DllImport("user32.dll", ExactSpelling = true, EntryPoint = "GetClassLongPtrW", SetLastError = true)]
    internal static extern nint GetClassLongPtr64(HWND hWnd, GET_CLASS_LONG_INDEX nIndex);
}
