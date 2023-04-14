// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using WinUIEx;

namespace DevHome.Common.Extensions;

/// <summary>
/// This class add extension methods to the <see cref="WindowEx"/> class.
/// </summary>
public static class WindowExExtensions
{
    /// <summary>
    /// Show an error message on the window.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="title">Dialog title.</param>
    /// <param name="content">Dialog content.</param>
    /// <param name="buttonText">Close button text.</param>
    public static async Task ShowErrorMessageDialogAsync(this WindowEx window, string title, string content, string buttonText)
    {
        await window.ShowMessageDialogAsync(dialog =>
        {
            dialog.Title = title;
            dialog.Content = new TextBlock()
            {
                Text = content,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.WrapWholeWords,
            };
            dialog.PrimaryButtonText = buttonText;
        });
    }

    /// <summary>
    /// Generic implementation for creating and displaying a message dialog on
    /// a window.
    ///
    /// This extension method overloads <see cref="WindowEx.ShowMessageDialogAsync"/>.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="action">Action performed on the created dialog.</param>
    private static async Task ShowMessageDialogAsync(this WindowEx window, Action<ContentDialog> action)
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
    public static void SetRequestedTheme(this WindowEx window, ElementTheme theme)
    {
        if (window.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
            TitleBarHelper.UpdateTitleBar(window, theme);
        }
    }

    /// <summary>
    /// Open file picker
    /// </summary>
    /// <param name="window">Target window</param>
    /// <param name="filters">List of filter name and extension</param>
    /// <returns>Storage file or <c>null</c> if no file was selected</returns>
    public static async Task<StorageFile?> OpenFilePickerAsync(this WindowEx window, List<string>? filters = null)
    {
        try
        {
            string fileName;

            // File picker fails when running the application as admin.
            // To workaround this issue, we instead use the Win32 picking APIs
            // as suggested in the documentation for the FileSavePicker:
            // >> Original code reference: https://learn.microsoft.com/en-us/uwp/api/windows.storage.pickers.filesavepicker?view=winrt-22621#in-a-desktop-app-that-requires-elevation
            // >> Github issue: https://github.com/microsoft/WindowsAppSDK/issues/2504
            // "In a desktop app (which includes WinUI 3 apps), you can use
            // FileSavePicker (and other types from Windows.Storage.Pickers).
            // But if the desktop app requires elevation to run, then you'll
            // need a different approach (that's because these APIs aren't
            // designed to be used in an elevated app). The code snippet below
            // illustrates how you can use the C#/Win32 P/Invoke Source
            // Generator (CsWin32) to call the Win32 picking APIs instead."
            unsafe
            {
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

                var hr = PInvoke.CoCreateInstance<IFileOpenDialog>(
                    typeof(FileOpenDialog).GUID,
                    null,
                    CLSCTX.CLSCTX_INPROC_SERVER,
                    out var fsd);

                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                // Set filters (e.g. "*.yaml", "*.yml", etc...)
                var extensions = new List<COMDLG_FILTERSPEC>();
                filters ??= new ();
                foreach (var filter in filters)
                {
                    COMDLG_FILTERSPEC extension;
                    extension.pszName = (char*)Marshal.StringToHGlobalUni(string.Empty);
                    extension.pszSpec = (char*)Marshal.StringToHGlobalUni(filter);
                    extensions.Add(extension);
                }

                // Generate last filter entry
                var allFilestString = Application.Current.GetService<IStringResource>().GetLocalized("AllFiles");
                var allTypes = filters.Any() ? string.Join(";", filters) : "*.*";
                COMDLG_FILTERSPEC allExtension;
                allExtension.pszName = (char*)Marshal.StringToHGlobalUni(allFilestString);
                allExtension.pszSpec = (char*)Marshal.StringToHGlobalUni(allTypes);
                extensions.Add(allExtension);

                fsd.SetFileTypes(extensions.ToArray());

                fsd.Show(new HWND(hWnd));
                fsd.GetResult(out var ppsi);

                PWSTR pFileName;
                ppsi.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, &pFileName);
                fileName = new string(pFileName);
            }

            return await StorageFile.GetFileFromPathAsync(fileName);
        }
        catch
        {
            // Return null if canceled or an error occurred
            return null;
        }
    }
}
