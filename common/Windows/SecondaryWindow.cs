// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using Microsoft.UI.Xaml;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinUIEx;

namespace DevHome.Common.Windows;

public class SecondaryWindow : WindowEx
{
    private readonly WindowTemplate _windowTemplate;
    private WindowEx? _primaryWindow;
    private bool _useAppTheme;
    private bool _isModal;
    private bool _isAlwaysOnTopOfPrimary;

    // Main Dev Home window
    private WindowEx MainWindow => Application.Current.GetService<WindowEx>();

    private IThemeSelectorService ThemeSelector => Application.Current.GetService<IThemeSelectorService>();

    public WindowTitleBar? WindowTitleBar
    {
        get => (WindowTitleBar)_windowTemplate.WindowTitleBarControl.Content;
        set => _windowTemplate.WindowTitleBarControl.Content = value;
    }

    /// <summary>
    /// Gets or sets the window content in the customized layout.
    /// </summary>
    public new object WindowContent
    {
        get => _windowTemplate.WindowContentControl.Content;
        set => _windowTemplate.WindowContentControl.Content = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the window should use the
    /// application theme and listen for theme changes.
    /// </summary>
    public bool UseAppTheme
    {
        get => _useAppTheme;
        set
        {
            if (_useAppTheme != value)
            {
                _useAppTheme = value;
                if (value)
                {
                    // Update the current theme for the window
                    this.SetRequestedTheme(ThemeSelector.Theme);

                    // Subscribe window to theme changes, and unsubscribe on window close
                    ThemeSelector.ThemeChanged += OnThemeChanged;
                    Closed += (_, _) => ThemeSelector.ThemeChanged -= OnThemeChanged;
                }
                else
                {
                    ThemeSelector.ThemeChanged -= OnThemeChanged;
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the window should be modal.
    /// </summary>
    public bool IsModal
    {
        get => _isModal;
        set
        {
            if (_isModal != value)
            {
                _isModal = value;
                if (PrimaryWindow != null)
                {
                    var hwnd = (HWND)PrimaryWindow.GetWindowHandle();
                    PInvoke.EnableWindow(hwnd, !value);
                }
            }
        }
    }

    public bool IsAlwaysOnTopOfPrimary
    {
        get => _isAlwaysOnTopOfPrimary;
        set
        {
            if (_isAlwaysOnTopOfPrimary != value)
            {
                _isAlwaysOnTopOfPrimary = value;
                if (PrimaryWindow != null)
                {
                    if (value)
                    {
                        PrimaryWindow.ZOrderChanged += OnPrimaryWindowZOrderChanged;
                    }
                    else
                    {
                        PrimaryWindow.ZOrderChanged -= OnPrimaryWindowZOrderChanged;
                    }
                }
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
                _primaryWindow.VisibilityChanged -= OnPrimaryWindowVisibilityChanged;
            }

            // Set new primary window
            _primaryWindow = value;

            if (_primaryWindow != null)
            {
                // Connect new primary window
                _primaryWindow.Closed += OnPrimaryWindowClosed;
                _primaryWindow.VisibilityChanged += OnPrimaryWindowVisibilityChanged;
            }
        }
    }

    public SecondaryWindow()
    {
        _windowTemplate = new (this);

        // By default, set the primary window as the main window
        Content = _windowTemplate;
        PrimaryWindow = MainWindow;
        Backdrop = new MicaSystemBackdrop();
        Activated += OnSecondaryWindowActivated;
        Closed += OnSecondaryWindowClosed;
    }

    /// <summary>
    /// Place the secondary window in the center of the <see cref="PrimaryWindow"/>.
    /// </summary>
    /// <remarks>See also: <seealso cref="WindowExtensions.CenterOnScreen"/></remarks>
    public void CenterOnWindow()
    {
        if (PrimaryWindow != null)
        {
            // Get DPI for primary widow
            const float defaultDPI = 96f;
            var dpi = HwndExtensions.GetDpiForWindow(PrimaryWindow.GetWindowHandle()) / defaultDPI;

            // Extract primary window dimensions
            var primaryWindowLeftOffset = PrimaryWindow.AppWindow.Position.X;
            var primaryWindowTopOffset = PrimaryWindow.AppWindow.Position.Y;
            var primaryWindowHalfWidth = (PrimaryWindow.Width * dpi) / 2;
            var primaryWindowHalfHeight = (PrimaryWindow.Height * dpi) / 2;

            // Derive secondary window dimensions
            var secondaryWindowHalfWidth = (Width * dpi) / 2;
            var secondaryWindowHalfHeight = (Height * dpi) / 2;
            var secondaryWindowLeftOffset = primaryWindowLeftOffset + primaryWindowHalfWidth - secondaryWindowHalfWidth;
            var secondaryWindowTopOffset = primaryWindowTopOffset + primaryWindowHalfHeight - secondaryWindowHalfHeight;

            // Move and resize secondary window
            this.MoveAndResize(secondaryWindowLeftOffset, secondaryWindowTopOffset, Width, Height);
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
        if (this.WindowTitleBar != null)
        {
            this.WindowTitleBar.IsActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }
    }

    private void OnSecondaryWindowClosed(object? sender, WindowEventArgs args)
    {
        if (PrimaryWindow != null)
        {
            // Unregister z-order change handler
            PrimaryWindow.ZOrderChanged -= OnPrimaryWindowZOrderChanged;
            Closed -= OnSecondaryWindowClosed;
            PrimaryWindow.Closed -= OnPrimaryWindowClosed;
            PrimaryWindow.VisibilityChanged -= OnPrimaryWindowVisibilityChanged;
            UseAppTheme = false;
            IsModal = false;
        }
    }

    private void OnPrimaryWindowVisibilityChanged(object? sender, WindowVisibilityChangedEventArgs args)
    {
        if (PrimaryWindow != null)
        {
            if (PrimaryWindow.Visible)
            {
                this.Show();
            }
            else
            {
                this.Minimize();
            }
        }
    }

    private void OnThemeChanged(object? sender, ElementTheme theme)
    {
        // Update the window theme
        this.SetRequestedTheme(theme);
    }
}
