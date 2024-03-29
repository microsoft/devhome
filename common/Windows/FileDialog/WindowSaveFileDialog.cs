// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml;
using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace DevHome.Common.Windows.FileDialog;

public class WindowSaveFileDialog : WindowFileDialog
{
    private IFileSaveDialog? _fileDialog;

    public WindowSaveFileDialog()
    {
        FileTypeChanged += OnFileTypeChanged;
    }

    private protected override IFileDialog CreateInstanceInternal()
    {
        PInvoke.CoCreateInstance<IFileSaveDialog>(typeof(FileSaveDialog).GUID, null, CLSCTX.CLSCTX_INPROC_SERVER, out _fileDialog).ThrowOnFailure();
        return _fileDialog;
    }

    private void OnFileTypeChanged(object? sender, WindowFileDialogFilter? fileType)
    {
        var fileName = GetFileName();
        fileName = GetModifiedFileName(fileName, fileType);
        SetFileName(fileName);
    }

    public string? Show(Window window)
    {
        Debug.Assert(_fileDialog != null, "The file dialog instance should not be null.");
        if (ShowInternal(window))
        {
            _fileDialog.GetResult(out var shellItem);
            var fileName = GetDisplayName(shellItem);
            return GetModifiedFileName(fileName, GetFileType());
        }

        return null;
    }

    private string GetModifiedFileName(string fileName, WindowFileDialogFilter? fileType)
    {
        // If the file type is null or has no patterns, return the file name as is
        if (fileType?.Patterns == null || fileType.Patterns.Count == 0)
        {
            return fileName;
        }

        // If the file name already ends with the file type extension, return the file name as is
        if (fileType.Patterns.Any(p => fileName.EndsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            return fileName;
        }

        // If the file name contains an extension from a different file type, remove it
        var allPatterns = GetFileTypes().SelectMany(ft => ft.Patterns);
        var matchPattern = allPatterns.FirstOrDefault(p => fileName.EndsWith(p, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        if (!string.IsNullOrEmpty(matchPattern))
        {
            fileName = fileName[..fileName.LastIndexOf(matchPattern, StringComparison.OrdinalIgnoreCase)];
        }

        // Append the first pattern from the selected file type
        return fileName + fileType.Patterns[0];
    }
}
