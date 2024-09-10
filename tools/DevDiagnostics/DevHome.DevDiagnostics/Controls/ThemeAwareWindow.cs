// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.ViewManagement;
using WinUIEx;

namespace DevHome.DevDiagnostics.Controls;

/* This class encapsulates the theme-awareness logic, including support for custom chrome buttons.
 * The main BarWindow derives from ThemeAwareWindow. All internal tool windows and any other owned windows
 * must follow the theme of the main BarWindow. To achieve this:
 * 1. Derive the tool window from ThemeAwareWindow.
 * 2. Define a grid or panel for the icon and title (because this window sets ExtendsContentIntoTitleBar
 *    and HideIconAndSystemMenu).
 * 3. Add the tool window to the list of related windows for the BarWindow by calling AddRelatedWindow.
 * 4. Remove the tool window from the list of related windows for the BarWindow when the tool window is closed.
 * 5. See ClipboardMonitoringWindow for an example.
 */

public class ThemeAwareWindow : WindowEx
{
    private readonly SolidColorBrush _darkModeActiveCaptionBrush;
    private readonly SolidColorBrush _darkModeInactiveCaptionBrush;
    private readonly SolidColorBrush _nonDarkModeActiveCaptionBrush;
    private readonly SolidColorBrush _nonDarkModeInactiveCaptionBrush;
    private readonly UISettings _uiSettings = new();
    private readonly DispatcherQueue _dispatcher;

    private WindowActivationState _currentActivationState = WindowActivationState.Deactivated;

    private List<ThemeAwareWindow> RelatedWindows { get; set; } = [];

    internal List<Button> CustomTitleBarButtons { get; private set; } = [];

    internal ElementTheme Theme { get; set; }

    public ThemeAwareWindow()
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();

        // Precreate the brushes for the caption buttons.
        // In Dark Mode, the active state is white, and the inactive state is translucent white.
        // In Light Mode, the active state is black, and the inactive state is translucent black.
        var color = Colors.White;
        _darkModeActiveCaptionBrush = new SolidColorBrush(color);
        color.A = 0x66;
        _darkModeInactiveCaptionBrush = new SolidColorBrush(color);

        color = Colors.Black;
        _nonDarkModeActiveCaptionBrush = new SolidColorBrush(color);
        color.A = 0x66;
        _nonDarkModeInactiveCaptionBrush = new SolidColorBrush(color);

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;

        Closed += ThemeAwareWindow_Closed;
        Activated += Window_Activated;
        _uiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;
    }

    // Invoked when the user changes system-wide personalization settings.
    private void UiSettings_ColorValuesChanged(UISettings sender, object args)
    {
        _dispatcher.TryEnqueue(ApplySystemThemeToCaptionButtons);
    }

    private void ThemeAwareWindow_Closed(object sender, WindowEventArgs args)
    {
        Activated -= Window_Activated;
        _uiSettings.ColorValuesChanged -= UiSettings_ColorValuesChanged;
    }

    internal void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        // This follows the design guidance of dimming our title bar elements when the window isn't activated.
        // https://learn.microsoft.com/en-us/windows/apps/develop/title-bar#dim-the-title-bar-when-the-window-is-inactive
        _currentActivationState = args.WindowActivationState;

        if (CustomTitleBarButtons.Count > 0)
        {
            UpdateCustomTitleBarButtonsTextColor();
        }
    }

    // Invoked when the user changes theme preference in this app's settings.
    internal void SetRequestedTheme(ElementTheme theme)
    {
        Theme = theme;

        if (Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;

            if (theme == ElementTheme.Dark)
            {
                SetCaptionButtonColors(Colors.White);
            }
            else if (theme == ElementTheme.Light)
            {
                SetCaptionButtonColors(Colors.Black);
            }
            else
            {
                ApplySystemThemeToCaptionButtons();
            }
        }

        foreach (var window in RelatedWindows)
        {
            window.SetRequestedTheme(theme);
        }
    }

    // AppWindow.TitleBar doesn't update caption button colors correctly when changed while app is running.
    // https://task.ms/44172495
    internal void ApplySystemThemeToCaptionButtons()
    {
        if (Content is FrameworkElement rootElement)
        {
            Color foregroundColor;
            if (rootElement.ActualTheme == ElementTheme.Dark)
            {
                foregroundColor = Colors.White;
            }
            else
            {
                foregroundColor = Colors.Black;
            }

            SetCaptionButtonColors(foregroundColor);
        }
    }

    internal void SetCaptionButtonColors(Color color)
    {
        AppWindow.TitleBar.ButtonForegroundColor = color;
        if (CustomTitleBarButtons.Count > 0)
        {
            UpdateCustomTitleBarButtonsTextColor();
        }
    }

    internal void UpdateCustomTitleBarButtonsTextColor()
    {
        var rootElement = Content as FrameworkElement;
        Debug.Assert(rootElement != null, "Expected Content to be a FrameworkElement");

        foreach (var button in CustomTitleBarButtons)
        {
            if (_currentActivationState == WindowActivationState.Deactivated)
            {
                var brush = (rootElement.ActualTheme == ElementTheme.Dark) ? _darkModeInactiveCaptionBrush : _nonDarkModeInactiveCaptionBrush;
                button.Foreground = brush;
            }
            else
            {
                var brush = (rootElement.ActualTheme == ElementTheme.Dark) ? _darkModeActiveCaptionBrush : _nonDarkModeActiveCaptionBrush;
                button.Foreground = brush;
            }
        }
    }

    internal void AddRelatedWindow(ThemeAwareWindow window)
    {
        RelatedWindows.Add(window);
        window.SetRequestedTheme(Theme);
    }

    internal void RemoveRelatedWindow(ThemeAwareWindow window)
    {
        RelatedWindows.Remove(window);
    }
}
