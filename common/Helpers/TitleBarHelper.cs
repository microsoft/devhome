// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

using Windows.UI;

namespace DevHome.Common.Helpers;

// Helper class to workaround custom title bar bugs.
// DISCLAIMER: The resource key names and color values used below are subject to change. Do not depend on them.
// https://github.com/microsoft/TemplateStudio/issues/4516
public class TitleBarHelper
{
    private const int WAINACTIVE = 0x00;
    private const int WAACTIVE = 0x01;
    private const int WMACTIVATE = 0x0006;

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

    public static void UpdateTitleBar(Window window, ElementTheme theme)
    {
        if (window.ExtendsContentIntoTitleBar)
        {
            if (theme != ElementTheme.Default)
            {
                Application.Current.Resources["WindowCaptionForeground"] = theme switch
                {
                    ElementTheme.Dark => new SolidColorBrush(Colors.White),
                    ElementTheme.Light => new SolidColorBrush(Colors.Black),
                    _ => new SolidColorBrush(Colors.Transparent)
                };

                Application.Current.Resources["WindowCaptionForegroundDisabled"] = theme switch
                {
                    ElementTheme.Dark => new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),
                    ElementTheme.Light => new SolidColorBrush(Color.FromArgb(0x66, 0x00, 0x00, 0x00)),
                    _ => new SolidColorBrush(Colors.Transparent)
                };

                Application.Current.Resources["WindowCaptionButtonBackgroundPointerOver"] = theme switch
                {
                    ElementTheme.Dark => new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                    ElementTheme.Light => new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0x00, 0x00)),
                    _ => new SolidColorBrush(Colors.Transparent)
                };

                Application.Current.Resources["WindowCaptionButtonBackgroundPressed"] = theme switch
                {
                    ElementTheme.Dark => new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),
                    ElementTheme.Light => new SolidColorBrush(Color.FromArgb(0x66, 0x00, 0x00, 0x00)),
                    _ => new SolidColorBrush(Colors.Transparent)
                };

                Application.Current.Resources["WindowCaptionButtonStrokePointerOver"] = theme switch
                {
                    ElementTheme.Dark => new SolidColorBrush(Colors.White),
                    ElementTheme.Light => new SolidColorBrush(Colors.Black),
                    _ => new SolidColorBrush(Colors.Transparent)
                };

                Application.Current.Resources["WindowCaptionButtonStrokePressed"] = theme switch
                {
                    ElementTheme.Dark => new SolidColorBrush(Colors.White),
                    ElementTheme.Light => new SolidColorBrush(Colors.Black),
                    _ => new SolidColorBrush(Colors.Transparent)
                };
            }

            Application.Current.Resources["WindowCaptionBackground"] = new SolidColorBrush(Colors.Transparent);
            Application.Current.Resources["WindowCaptionBackgroundDisabled"] = new SolidColorBrush(Colors.Transparent);
        }
    }

    /// <summary>
    /// Gets the title bar text color brush based on the window activation state.
    /// </summary>
    /// <param name="isWindowActive">Window activation state</param>
    /// <returns>Corresponding color brush for the title bar text</returns>
    public static SolidColorBrush GetTitleBarTextColorBrush(bool isWindowActive)
    {
        var resource = isWindowActive ? "WindowCaptionForeground" : "WindowCaptionForegroundDisabled";
        return (SolidColorBrush)Application.Current.Resources[resource];
    }
}
