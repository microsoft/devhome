// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Contracts.Services;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace DevHome.Common.Extensions;

/// <summary>
/// Secondary window class derives from <see cref="WindowEx"/> and should be
/// extended by all secondary window classes in the application to ensure a
/// consistent window look, theme and behavior.
/// </summary>
public partial class SecondaryWindowExtension : WindowEx
{
    private WindowEx? _primaryWindow;
    private bool _useAppTheme;

    /// <summary>
    /// Gets or sets a value indicating whether the window should use the
    /// application theme and listen for theme changes.
    /// </summary>
    public bool UseAppTheme
    {
        get => _useAppTheme;
        set
        {
            _useAppTheme = value;
            var themeSelector = Application.Current.GetService<IThemeSelectorService>();
            if (_useAppTheme)
            {
                // Update the current theme for the window
                this.SetRequestedTheme(themeSelector.Theme);

                // Subscribe window to theme changes, and unsubscribe on window close
                themeSelector.ThemeChanged += OnThemeChanged;
                Closed += (_, _) => themeSelector.ThemeChanged -= OnThemeChanged;
            }
            else
            {
                themeSelector.ThemeChanged -= OnThemeChanged;
            }
        }
    }

    /// <summary>
    /// Gets or sets the primary window.
    /// </summary>
    public WindowEx? PrimaryWindow
    {
        get => _primaryWindow;
        set
        {
            // If a previous primary window was connected, disconnect it
            if (_primaryWindow != null)
            {
                _primaryWindow.Closed -= OnPrimaryWindowClosed;
                _primaryWindow.VisibilityChanged -= OnPrimaryWindowVisiblityChanged;
            }

            // Set new primary window
            _primaryWindow = value;

            if (_primaryWindow != null)
            {
                // Connect new primary window
                _primaryWindow.Closed += OnPrimaryWindowClosed;
                _primaryWindow.VisibilityChanged += OnPrimaryWindowVisiblityChanged;
            }
        }
    }

    public SecondaryWindowExtension()
    {
        // By default, set the primary window as the main window
        PrimaryWindow = Application.Current.GetService<WindowEx>();
        Backdrop = PrimaryWindow.Backdrop;
        Closed += OnSecondaryWindowClosed;
        Activated += OnSecondaryWindowActivated;
        CenterAndElevateWindow();
    }

    public void CenterAndElevateWindow()
    {
        if (PrimaryWindow != null)
        {
            // first get a quarter of the size of the of the current window then get the center point
            // of the primary window. Substract primary windows Y by a quarter of the secondary windows
            // Y to move the secondary window upwards. This will show the secondary winddow in the center
            // of the app, slightly elevated above the content.
            var secondaryY = Height / 4D;
            var primaryX = (double)PrimaryWindow.AppWindow.Position.X;
            var primaryY = (double)PrimaryWindow.AppWindow.Position.Y;
            primaryX += PrimaryWindow.Width / 2D;
            primaryY += PrimaryWindow.Height / 2D;
            this.MoveAndResize(primaryX, primaryY - secondaryY, Width, Height);
        }
    }

    public void OnPrimaryWindowClosed(object? sender, WindowEventArgs args)
    {
        // Close secondary window
        Close();
    }

    private void OnPrimaryWindowZOrderChanged(object? sender, ZOrderInfo args)
    {
        // Bring secondary window to front
        BringToFront();
    }

    private void OnSecondaryWindowActivated(object? sender, WindowActivatedEventArgs args)
    {
        if (PrimaryWindow != null)
        {
            // Keep secondary window in front
            PrimaryWindow.ZOrderChanged += OnPrimaryWindowZOrderChanged;
        }

        // Activated is called several times on different WindowActivationState
        Activated -= OnSecondaryWindowActivated;
    }

    private void OnSecondaryWindowClosed(object? sender, WindowEventArgs args)
    {
        if (PrimaryWindow != null)
        {
            // Unregister z-order change handler
            PrimaryWindow.ZOrderChanged -= OnPrimaryWindowZOrderChanged;
            Closed -= OnSecondaryWindowClosed;
        }
    }

    public void OnPrimaryWindowVisiblityChanged(object? sender, WindowVisibilityChangedEventArgs args)
    {
        // Close secondary window
        if (PrimaryWindow != null && !PrimaryWindow.Visible)
        {
            this.Minimize();
        }
        else
        {
            this.Show();
        }
    }

    public void OnThemeChanged(object? sender, ElementTheme theme)
    {
        // Update the window theme
        this.SetRequestedTheme(theme);
    }
}
