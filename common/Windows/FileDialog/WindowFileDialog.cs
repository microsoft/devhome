// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using WinRT.Interop;

namespace DevHome.Common.Windows.FileDialog;

/// <remarks>
/// File picker fails when running the application as admin.
/// To workaround this issue, we instead use the Win32 picking APIs
/// as suggested in the documentation for the FileSavePicker:
/// >> Original code reference: https://learn.microsoft.com/uwp/api/windows.storage.pickers.filesavepicker?view=winrt-22621#in-a-desktop-app-that-requires-elevation
/// >> GitHub issue: https://github.com/microsoft/WindowsAppSDK/issues/2504
/// "In a desktop app (which includes WinUI 3 apps), you can use
/// FileSavePicker (and other types from Windows.Storage.Pickers).
/// But if the desktop app requires elevation to run, then you'll
/// need a different approach (that's because these APIs aren't
/// designed to be used in an elevated app). The code snippet below
/// illustrates how you can use the C#/Win32 P/Invoke Source
/// Generator (CsWin32) to call the Win32 picking APIs instead."
/// </remarks>
public abstract class WindowFileDialog : IDisposable
{
    // File dialog error code
    private const int FilePickerCanceledErrorCode = unchecked((int)0x800704C7);

    private readonly IFileDialog _fileDialog;
    private readonly uint _adviseCookie;
    private List<WindowFileDialogFilter> _fileTypes = [];
    private bool disposedValue;

    // File dialog events
    public event EventHandler<IWindowFileDialogFilter?>? FileTypeChanged;

    public WindowFileDialog()
    {
        _fileDialog = CreateInstanceInternal();
        _fileDialog.Advise(new WindowFileDialogEvents(this), out _adviseCookie);
    }

    /// <summary>
    /// Gets the file types that the user can choose from.
    /// </summary>
    /// <returns>List of file types.</returns>
    public IReadOnlyCollection<IWindowFileDialogFilter> GetFileTypes() => _fileTypes;

    /// <summary>
    /// Sets the file types that the user can choose from.
    /// </summary>
    /// <param name="fileTypes">List of file types.</param>
    public void SetFileTypes(List<(string, List<string>)> fileTypes)
    {
        // Dispose previous file types
        _fileTypes.ForEach(ft => ft.Dispose());

        // Set new file types
        _fileTypes = fileTypes.Select(ft => new WindowFileDialogFilter(ft.Item1, [..ft.Item2])).ToList() ?? [];
        var extensions = _fileTypes.Select(ft => ft.Extension).ToList();
        _fileDialog.SetFileTypes(CollectionsMarshal.AsSpan(extensions));
    }

    /// <summary>
    /// Gets the file name that the user has chosen.
    /// </summary>
    /// <returns>File name.</returns>
    public unsafe string GetFileName()
    {
        // Get the file name and free the memory
        _fileDialog.GetFileName(out var pFileName);
        var fileName = new string(pFileName);
        Marshal.FreeCoTaskMem((IntPtr)pFileName.Value);
        return fileName;
    }

    /// <summary>
    /// Sets the file name programmatically.
    /// </summary>
    /// <param name="fileName">File name.</param>
    public void SetFileName(string fileName) => _fileDialog.SetFileName(fileName);

    /// <summary>
    /// Gets the file name that the user has chosen.
    /// </summary>
    /// <returns>File type.</returns>
    public IWindowFileDialogFilter? GetFileType() => _fileTypes.ElementAtOrDefault(GetFileTypeIndex());

    /// <inheritdoc />
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Shows the file dialog.
    /// </summary>
    /// <param name="window">Window to show the dialog on.</param>
    /// <returns>True if the user has chosen a file; otherwise, false if the user has canceled the dialog.</returns>
    protected bool ShowInternal(Window window)
    {
        try
        {
            var hWnd = WindowNative.GetWindowHandle(window);
            _fileDialog.Show(new HWND(hWnd));
            return true;
        }
        catch (COMException e) when (e.ErrorCode == FilePickerCanceledErrorCode)
        {
            return false;
        }
    }

    /// <summary>
    /// Creates an instance of the file dialog.
    /// </summary>
    /// <returns>File dialog instance.</returns>
    private protected abstract IFileDialog CreateInstanceInternal();

    /// <summary>
    /// Gets the display name of the shell item.
    /// </summary>
    /// <param name="shellItem">Shell item.</param>
    /// <returns>Display name.</returns>
    private protected static unsafe string GetDisplayName(IShellItem? shellItem)
    {
        // Get the display name and free the memory
        shellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var pFileName);
        var fileName = new string(pFileName);
        Marshal.FreeCoTaskMem((IntPtr)pFileName.Value);
        return fileName;
    }

    /// <summary>
    /// Get the file type index selected by the user.
    /// </summary>
    /// <returns>File type index.</returns>
    private int GetFileTypeIndex()
    {
        // NOTE: IFileDialog::GetFileTypeIndex method is a one-based
        // index rather than zero-based.
        _fileDialog.GetFileTypeIndex(out var uFileTypeIndex);
        return (int)uFileTypeIndex - 1;
    }

    /// <inheritdoc cref="Dispose()"/>/>
    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _fileTypes.ForEach((filter) => filter.Dispose());
                _fileDialog.Unadvise(_adviseCookie);
            }

            disposedValue = true;
        }
    }

    /// <summary>
    /// Class to handle file dialog events.
    /// </summary>
    private sealed class WindowFileDialogEvents(WindowFileDialog fileDialog) : IFileDialogEvents
    {
        private readonly WindowFileDialog _fileDialog = fileDialog;

        public void OnTypeChange(IFileDialog pfd) => _fileDialog.FileTypeChanged?.Invoke(null, _fileDialog.GetFileType());

        /************************************************************
         * Redirect more events here if needed                      *
         ************************************************************/

        public void OnFileOk(IFileDialog pfd) => Expression.Empty();

        public void OnFolderChanging(IFileDialog pfd, IShellItem psiFolder) => Expression.Empty();

        public void OnFolderChange(IFileDialog pfd) => Expression.Empty();

        public void OnSelectionChange(IFileDialog pfd) => Expression.Empty();

        public unsafe void OnShareViolation(IFileDialog pfd, IShellItem psi, FDE_SHAREVIOLATION_RESPONSE* pResponse) => Expression.Empty();

        public unsafe void OnOverwrite(IFileDialog pfd, IShellItem psi, FDE_OVERWRITE_RESPONSE* pResponse) => Expression.Empty();
    }
}
