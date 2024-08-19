// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Serilog;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace DevHome.Common.Windows.FileDialog;

public class WindowOpenFolderDialog : WindowFileDialog
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WindowOpenFolderDialog));

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
        try
        {
            if (ShowOk(window))
            {
                FileDialog.GetResult(out var shellItem);
                var folderPath = GetDisplayName(shellItem);

                // GetFolderFromPathAsync will throw if the user does not have access to the
                // folder.  One such example is a hidden folder.
                return await StorageFolder.GetFolderFromPathAsync(folderPath).AsTask();
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex.ToString());
        }

        return null;
    }
}
