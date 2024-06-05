// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace DevHome.Common.Windows.FileDialog;

/// <summary>
/// Represents a window that allows the user to open a file.
/// </summary>
public class WindowOpenFileDialog : WindowFileDialog
{
    /// <inheritdoc />
    private protected override IFileDialog CreateInstance()
    {
        PInvoke.CoCreateInstance<IFileOpenDialog>(typeof(FileOpenDialog).GUID, null, CLSCTX.CLSCTX_INPROC_SERVER, out var fileDialog).ThrowOnFailure();
        return fileDialog;
    }

    /// <summary>
    /// Shows the file dialog and returns the selected file.
    /// </summary>
    /// <param name="window">The window to parent the file dialog.</param>
    /// <returns>The selected file or <see langword="null"/> if no file was selected.</returns>
    public async Task<StorageFile?> ShowAsync(Window window)
    {
        if (ShowOk(window))
        {
            FileDialog.GetResult(out var shellItem);
            var fileName = GetDisplayName(shellItem);
            return await StorageFile.GetFileFromPathAsync(fileName).AsTask();
        }

        return null;
    }
}
