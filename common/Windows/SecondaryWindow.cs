// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using Microsoft.UI.Xaml;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;

namespace DevHome.Common.Windows;

public class SecondaryWindow : WindowEx
{
    private readonly WindowTemplate _windowTemplate;
    private WindowEx? _primaryWindow;
    private bool _useAppTheme;
    private bool _isModal;
    private bool _hasOwner;

    private WindowEx MainWindow => Application.Current.GetService<WindowEx>();

    private IThemeSelectorService ThemeSelector => Application.Current.GetService<IThemeSelectorService>();

    private IAppInfoService AppInfo => Application.Current.GetService<IAppInfoService>();

    public WindowTitleBar? WindowTitleBar
    {
        get => _windowTemplate.TitleBar;
        set
        {
            if (WindowTitleBar != value)
            {
                // Remove title changed event handler from previous title bar
                if (WindowTitleBar != null)
                {
                    WindowTitleBar.TitleChanged -= OnSecondaryWindowTitleChanged;
                }

                // Set new title bar and update window title
                _windowTemplate.TitleBar = value;
                OnSecondaryWindowTitleChanged(null, value?.Title);

                // Add title changed event handler to new title bar
                if (value != null)
                {
                    value.TitleChanged += OnSecondaryWindowTitleChanged;
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the window content in the custom layout.
    /// </summary>
    public new object WindowContent
    {
        get => _windowTemplate.MainContent;
        set => _windowTemplate.MainContent = value;
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
                    OnThemeChanged(null, ThemeSelector.Theme);
                    ThemeSelector.ThemeChanged += OnThemeChanged;
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
    /// <remarks>Setting this property to true will disable interaction on the primary window.</remarks>
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
                    PInvoke.EnableWindow((HWND)PrimaryWindow.GetWindowHandle(), !value);
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="PrimaryWindow"/>
    /// should be the owner of the secondary window.
    /// </summary>
    /// <remarks>Setting this property to true will keep the secondary window on top of the primary window.</remarks>
    public bool HasOwner
    {
        get => _hasOwner;
        set
        {
            if (_hasOwner != value)
            {
                _hasOwner = value;

                // Set primary window as owner of secondary window
                var child = (HWND)this.GetWindowHandle();
                var parent = (HWND?)PrimaryWindow?.GetWindowHandle() ?? HWND.Null;
                PInvoke.SetWindowLongPtr(child, WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, value ? parent : HWND.Null);
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
            if (_primaryWindow != value)
            {
                // If a previous primary window was connected, disconnect it
                if (_primaryWindow != null)
                {
                    _primaryWindow.Closed -= OnPrimaryWindowClosed;
                }

                // Set new primary window
                _primaryWindow = value;

                // If a primary window is set, connect it
                if (_primaryWindow != null)
                {
                    _primaryWindow.Closed += OnPrimaryWindowClosed;
                }
            }
        }
    }

    public SecondaryWindow()
    {
        // Initialize window content template
        _windowTemplate = new (this);
        Content = _windowTemplate;

        // Register secondary window events handlers
        Activated += OnSecondaryWindowActivated;
        Closed += OnSecondaryWindowClosed;

        // Set default window configuration
        PrimaryWindow = MainWindow;
        SystemBackdrop = PrimaryWindow.SystemBackdrop;
        UseAppTheme = true;
        Title = AppInfo.GetAppNameLocalized();
        this.SetIcon(AppInfo.IconPath);

        ShowInTaskbar();
    }

    /// <summary>
    /// If the primary window is set, center the secondary window on the
    /// primary window. Otherwise, center the secondary window on the screen.
    /// </summary>
    /// <remarks>
    /// <para>This method should be called after the secondary window is shown.</para>
    /// <para>See also: <seealso cref="WindowExtensions.CenterOnScreen"/></para>
    /// </remarks>
    public void CenterOnWindow()
    {
        if (PrimaryWindow == null)
        {
            this.CenterOnScreen();
        }
        else
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

    /// <summary>
    /// Show secondary window in taskbar.
    /// </summary>
    /// <remarks>
    /// This is specifically required when a window owner is set, where by
    /// default the secondary window is not visible in the taskbar.
    /// </remarks>
    private void ShowInTaskbar()
    {
        var hwnd = (HWND)this.GetWindowHandle();
        var exStyle = PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE) | (int)WINDOW_EX_STYLE.WS_EX_APPWINDOW;
        _ = PInvoke.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, exStyle);
    }

    /**********************************************************
     *  Application services event handlers                   *
     *  Note: Application services event must be unregistered *
     *        when the secondary window is closed.            *
     **********************************************************/

    private void OnThemeChanged(object? sender, ElementTheme theme)
    {
        // Update the window theme
        this.SetRequestedTheme(theme);
    }

    /**************************************************************
     *  Primary window event handlers                             *
     *  Note: Primary window event handlers must be unregistered  *
     *        when the secondary window is closed.                *
     **************************************************************/

    private void OnPrimaryWindowClosed(object? sender, WindowEventArgs args)
    {
        // Close secondary window
        Close();
    }

    /*************************************
     *  Secondary window event handlers  *
     ************************************/

    private void OnSecondaryWindowActivated(object? sender, WindowActivatedEventArgs args)
    {
        // Reflect window activation state in title bar
        if (this.WindowTitleBar != null)
        {
            this.WindowTitleBar.IsActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }
    }

    private void OnSecondaryWindowClosed(object? sender, WindowEventArgs args)
    {
        // Free the secondary window from the application
        IsModal = false;
        HasOwner = false;
        UseAppTheme = false;

        // Unset the primary window at the end
        PrimaryWindow = null;
    }

    private void OnSecondaryWindowTitleChanged(object? sender, string? title)
    {
        // Window title in taskbar
        Title = string.IsNullOrEmpty(title) ? AppInfo.GetAppNameLocalized() : title;

        // Window title bar text
        if (WindowTitleBar != null)
        {
            WindowTitleBar.Title = Title;
        }
    }
}
