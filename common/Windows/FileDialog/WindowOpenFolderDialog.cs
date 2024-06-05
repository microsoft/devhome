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

public class WindowOpenFolderDialog : WindowFileDialog
{
    /// <inheritdoc />
    private protected override IFileDialog CreateInstance()
    {
        PInvoke.CoCreateInstance<IFileOpenDialog>(typeof(FileOpenDialog).GUID, null, CLSCTX.CLSCTX_INPROC_SERVER, out var fileDialog).ThrowOnFailure();
        return fileDialog;
    }

    /// <inheritdoc />
    protected override void InitializeInstance()
    {
        AddOption(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);
    }

    public async Task<StorageFolder?> ShowAsync(Window window)
    {
        if (ShowOk(window))
        {
            FileDialog.GetResult(out var shellItem);
            var folderPath = GetDisplayName(shellItem);
            return await StorageFolder.GetFolderFromPathAsync(folderPath).AsTask();
        }

        return null;
    }
}
