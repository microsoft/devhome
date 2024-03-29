// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace DevHome.Common.Windows.FileDialog;

public class WindowOpenFileDialog : WindowFileDialog
{
    private IFileOpenDialog? _fileDialog;

    private protected override IFileDialog CreateInstanceInternal()
    {
        PInvoke.CoCreateInstance<IFileOpenDialog>(typeof(FileOpenDialog).GUID, null, CLSCTX.CLSCTX_INPROC_SERVER, out _fileDialog).ThrowOnFailure();
        return _fileDialog;
    }

    public async Task<StorageFile?> ShowAsync(Window window)
    {
        Debug.Assert(_fileDialog != null, "The file dialog instance should not be null.");
        if (ShowInternal(window))
        {
            _fileDialog.GetResult(out var shellItem);
            var fileName = GetDisplayName(shellItem);
            return await StorageFile.GetFileFromPathAsync(fileName).AsTask();
        }

        return null;
    }
}
