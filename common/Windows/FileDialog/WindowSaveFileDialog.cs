// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace DevHome.Common.Windows.FileDialog;

/// <summary>
/// Represents a window that allows the user to save a file.
/// </summary>
public class WindowSaveFileDialog : WindowFileDialog
{
    /// <inheritdoc />
    private protected override IFileDialog CreateInstance()
    {
        // Create FileSaveDialog instance
        PInvoke.CoCreateInstance<IFileSaveDialog>(typeof(FileSaveDialog).GUID, null, CLSCTX.CLSCTX_INPROC_SERVER, out var fileDialog).ThrowOnFailure();
        return fileDialog;
    }

    /// <inheritdoc />
    protected override void InitializeInstance()
    {
        FileTypeChanged += OnFileTypeChanged;
    }

    /// <summary>
    /// On file type changed event handler.
    /// </summary>
    /// <param name="sender">Sender object.</param>
    /// <param name="fileType">Selected file type.</param>
    private void OnFileTypeChanged(object? sender, IWindowFileDialogFilter? fileType)
    {
        FileName = GetModifiedFileName(FileName, fileType);
    }

    /// <summary>
    /// Shows the save file dialog.
    /// </summary>
    /// <param name="window">The parent window.</param>
    /// <returns>The selected file path.</returns>
    public string? Show(Window window)
    {
        if (ShowOk(window))
        {
            FileDialog.GetResult(out var shellItem);
            var filePath = GetDisplayName(shellItem);
            return GetModifiedFileName(filePath, FileType);
        }

        return null;
    }

    /// <summary>
    /// Get the modified file name based on the selected file type.
    /// </summary>
    /// <param name="fileName">File name.</param>
    /// <param name="fileType">File type.</param>
    /// <returns>Modified file name.</returns>
    private string GetModifiedFileName(string fileName, IWindowFileDialogFilter? fileType)
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
        var allPatterns = AvailableFileTypes.SelectMany(ft => ft.Patterns);
        var matchPattern = allPatterns.FirstOrDefault(p => fileName.EndsWith(p, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        if (!string.IsNullOrEmpty(matchPattern))
        {
            fileName = fileName[..fileName.LastIndexOf(matchPattern, StringComparison.OrdinalIgnoreCase)];
        }

        // Append the first pattern from the selected file type
        return fileName + fileType.Patterns[0];
    }
}
