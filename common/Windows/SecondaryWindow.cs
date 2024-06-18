// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DevHome.Common.Windows;

[ContentProperty(Name = nameof(SecondaryWindowContent))]
public class SecondaryWindow : WinUIEx.WindowEx
{
    private readonly SecondaryWindowTemplate _windowTemplate;
    private Window? _primaryWindow;
    private bool _useAppTheme;
    private bool _isModal;
    private bool _isTopLevel;

    private Window MainWindow => Application.Current.GetService<Window>();

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

                // Set new title bar
                _windowTemplate.TitleBar = value;

                // By default, if no title is set, use the application name as title
                var title = value?.Title;
                title = string.IsNullOrEmpty(title) ? AppInfo.GetAppNameLocalized() : title;
                OnSecondaryWindowTitleChanged(null, title);

                // Add title changed event handler to new title bar
                if (value != null)
                {
                    value.Title = title;
                    value.TitleChanged += OnSecondaryWindowTitleChanged;
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the window content in the custom layout.
    /// </summary>
    /// <remarks>
    /// This is the default content of the secondary window.
    /// See also <seealso cref="ContentPropertyAttribute"/>.
    /// </remarks>
    public object SecondaryWindowContent
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
                    // Note: This is a temporary workaround until there's a
                    // built-in support for modal windows in WinUI 3.
                    PInvoke.EnableWindow((HWND)PrimaryWindow.GetWindowHandle(), !value);
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the secondary window should stay on top of the primary window.
    /// </summary>
    public bool IsTopLevel
    {
        get => _isTopLevel;
        set
        {
            if (_isTopLevel != value)
            {
                _isTopLevel = value;

                /*
                 * Note: Setting the owner here is a temporary workaround until there's is
                 * a built-in support for creating secondary windows with an owner while
                 * also being able to customize the content from XAML in WinUI 3.
                 * Related: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/windowing/windowing-overview#limitations
                 * "The Windows App SDK doesn't currently provide methods for attaching UI framework content to an AppWindow."
                 */

                // Set primary window as owner of secondary window
                var sWindow = (HWND)this.GetWindowHandle();
                var pWindow = (HWND?)PrimaryWindow?.GetWindowHandle() ?? HWND.Null;
                SetWindowOwner(sWindow, value ? pWindow : HWND.Null);
            }
        }
    }

    public Window? PrimaryWindow
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
        // Set the theme of the secondary window before creating the window template.
        // This must be done before the creation of the template, or else the secondary window
        // will continue to follow the Windows system theme, and not respect the theme
        // set by the app itself.
        UseAppTheme = true;

        // Initialize window content template
        _windowTemplate = new(this);
        Content = _windowTemplate;

        // Register secondary window events handlers
        Activated += OnSecondaryWindowActivated;
        Closed += OnSecondaryWindowClosed;

        // Set default window configuration
        PrimaryWindow = MainWindow;
        SystemBackdrop = PrimaryWindow.SystemBackdrop;

        Title = AppInfo.GetAppNameLocalized();
        AppWindow.SetIcon(AppInfo.IconPath);

        ShowInTaskbar();
    }

    public SecondaryWindow(object secondaryWindowContent)
        : this()
    {
        SecondaryWindowContent = secondaryWindowContent;
    }

    /// <summary>
    /// If the primary window is set, center the secondary window on the
    /// primary window. Otherwise, center the secondary window on the screen.
    /// </summary>
    /// <remarks>
    /// <para>This method should be called after the secondary window is shown.</para>
    /// <para>See also: <seealso cref="WindowExExtensions.CenterOnScreen"/></para>
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
            var dpi = PInvoke.GetDpiForWindow((HWND)PrimaryWindow.GetWindowHandle()) / defaultDPI;

            // Extract primary window dimensions
            var primaryWindowLeftOffset = PrimaryWindow.AppWindow.Position.X;
            var primaryWindowTopOffset = PrimaryWindow.AppWindow.Position.Y;
            var primaryWindowHalfWidth = (PrimaryWindow.AppWindow.Size.Width * dpi) / 2;
            var primaryWindowHalfHeight = (PrimaryWindow.AppWindow.Size.Height * dpi) / 2;

            // Derive secondary window dimensions
            var secondaryWindowHalfWidth = (AppWindow.Size.Width * dpi) / 2;
            var secondaryWindowHalfHeight = (AppWindow.Size.Height * dpi) / 2;
            var secondaryWindowLeftOffset = primaryWindowLeftOffset + primaryWindowHalfWidth - secondaryWindowHalfWidth;
            var secondaryWindowTopOffset = primaryWindowTopOffset + primaryWindowHalfHeight - secondaryWindowHalfHeight;

            // Move and resize secondary window
            var newRect = new RectInt32((int)secondaryWindowLeftOffset, (int)secondaryWindowTopOffset, AppWindow.Size.Width, AppWindow.Size.Height);
            AppWindow.MoveAndResize(newRect);
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

    private static void SetWindowOwner(HWND secondaryWindow, HWND primaryWindow)
    {
        // On x64 platform (IntPtr.Size = 8), call SetWindowLongPtr
        // On x32 platform (IntPtr.Size = 4), call SetWindowLong
        // Reference: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowlongptra
        // Note: CsWin32 will not generate PInvoke.SetWindowLongPtr for x86 platform which will cause a compilation error.
        if (IntPtr.Size == 8)
        {
            SetWindowLongPtr(secondaryWindow, WINDOW_LONG_PTR_INDEX.GWLP_HWNDPARENT, primaryWindow);
        }
        else
        {
            _ = PInvoke.SetWindowLong(secondaryWindow, WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, primaryWindow.Value.ToInt32());
        }
    }

    /// <remarks>
    /// This method cannot be added to the NativeMethods.txt because it is not
    /// available when compiling the solution for x86 platform.
    /// </remarks>
    [DllImport("user32.dll", ExactSpelling = true, EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, nint dwNewLong);

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
        IsTopLevel = false;
        UseAppTheme = false;

        // Unset the primary window at the end
        PrimaryWindow = null;
    }

    private void OnSecondaryWindowTitleChanged(object? sender, string? title)
    {
        // Update window title (e.g. in taskbar)
        Title = title ?? string.Empty;
    }
}
