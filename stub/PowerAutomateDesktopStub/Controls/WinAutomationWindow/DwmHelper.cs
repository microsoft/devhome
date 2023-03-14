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
		#region Fields/Consts

		private const int GWL_STYLE = -16;
		private const int WS_MAXIMIZEBOX = 0x10000;

		#endregion

		#region Externs

		[DllImport("user32.dll")]
		private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll")]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		#endregion

		#region Methods

		public static void DisableMaximize(Window window)
		{
			var hwnd = new WindowInteropHelper(window).Handle;
			var value = GetWindowLong(hwnd, GWL_STYLE);
			SetWindowLong(hwnd, GWL_STYLE, value & ~WS_MAXIMIZEBOX);
		}

		#endregion
	}
}