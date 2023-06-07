// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DevHome.Common.Helpers;
using DevHome.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
    public const int FilePickerCanceledErrorCode = unchecked((int)0x800704C7);

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
    /// <param name="filters">List of type filters (e.g. *.yaml, *.txt), or empty/<c>null</c> to allow all file types</param>
    /// <returns>Storage file or <c>null</c> if no file was selected</returns>
    public static async Task<StorageFile?> OpenFilePickerAsync(this WindowEx window, Logger? logger, params (string Type, string Name)[] filters)
    {
        try
        {
            if (filters.Length == 0)
            {
                throw new ArgumentException("Input filters cannot be empty");
            }

            string fileName;

            // File picker fails when running the application as admin.
            // To workaround this issue, we instead use the Win32 picking APIs
            // as suggested in the documentation for the FileSavePicker:
            // >> Original code reference: https://learn.microsoft.com/uwp/api/windows.storage.pickers.filesavepicker?view=winrt-22621#in-a-desktop-app-that-requires-elevation
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
                Marshal.ThrowExceptionForHR(hr);

                IShellItem ppsi;
                var extensions = new List<COMDLG_FILTERSPEC>();

                try
                {
                    // Set filters (e.g. "*.yaml", "*.yml", etc...)
                    foreach (var filter in filters)
                    {
                        COMDLG_FILTERSPEC extension;
                        extension.pszName = (char*)Marshal.StringToHGlobalUni(filter.Name);
                        extension.pszSpec = (char*)Marshal.StringToHGlobalUni(filter.Type);
                        extensions.Add(extension);
                    }

                    fsd.SetFileTypes(CollectionsMarshal.AsSpan(extensions));

                    fsd.Show(new HWND(hWnd));
                    fsd.GetResult(out ppsi);
                }
                finally
                {
                    // Free all filter names and specs
                    foreach (var extension in extensions)
                    {
                        Marshal.FreeHGlobal((IntPtr)extension.pszName.Value);
                        Marshal.FreeHGlobal((IntPtr)extension.pszSpec.Value);
                    }
                }

                // Get the display name and then manually free it after creating the string.
                // See https://learn.microsoft.com/windows/win32/api/shobjidl_core/nf-shobjidl_core-ishellitem-getdisplayname:
                // "It is the responsibility of the caller to free the string pointed to by ppszName
                // when it is no longer needed. Call CoTaskMemFree on *ppszName to free the memory."
                PWSTR pFileName;
                ppsi.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, &pFileName);
                fileName = new string(pFileName);
                Marshal.FreeCoTaskMem((IntPtr)pFileName.Value);
            }

            return await StorageFile.GetFileFromPathAsync(fileName);
        }
        catch (COMException e) when (e.ErrorCode == FilePickerCanceledErrorCode)
        {
            // No-op: Operation was canceled by the user
            return null;
        }
        catch (Exception e)
        {
            logger?.ReportError("File picker failed. Returning null.", e);
            return null;
        }
    }
}
