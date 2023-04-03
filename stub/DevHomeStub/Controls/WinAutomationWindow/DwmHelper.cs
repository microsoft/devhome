// ----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Microsoft.Flow.RPA.Desktop.Shared.UI.Controls.WinAutomationWindow
{
    public static class DwmHelper
    {
        private const int GWLStyle = -16;
        private const int WSMaximizeBox = 0x10000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public static void DisableMaximize(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            var value = GetWindowLong(hwnd, GWLStyle);
            SetWindowLong(hwnd, GWLStyle, value & ~WSMaximizeBox);
        }
    }
}
