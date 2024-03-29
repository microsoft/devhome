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
    public event EventHandler<WindowFileDialogFilter?>? FileTypeChanged;

    public WindowFileDialog()
    {
        _fileDialog = CreateInstanceInternal();
        _fileDialog.Advise(new WindowFileDialogEvents(this), out _adviseCookie);
    }

    public List<WindowFileDialogFilter> GetFileTypes() => _fileTypes;

    public void SetFileTypes(List<WindowFileDialogFilter> fileTypes)
    {
        // Dispose previous file types
        _fileTypes.ForEach(ft => ft.Dispose());

        // Set new file types
        _fileTypes = fileTypes ?? [];
        var extensions = _fileTypes.Select(ft => ft.Extension).ToList();
        _fileDialog.SetFileTypes(CollectionsMarshal.AsSpan(extensions));
    }

    public unsafe string GetFileName()
    {
        _fileDialog.GetFileName(out var pFileName);
        var fileName = new string(pFileName);
        Marshal.FreeCoTaskMem((IntPtr)pFileName.Value);
        return fileName;
    }

    public void SetFileName(string fileName) => _fileDialog.SetFileName(fileName);

    public WindowFileDialogFilter? GetFileType() => _fileTypes.ElementAtOrDefault(GetFileTypeIndex());

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

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

    private protected abstract IFileDialog CreateInstanceInternal();

    private protected static unsafe string GetDisplayName(IShellItem? shellItem)
    {
        shellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var pFileName);
        var fileName = new string(pFileName);
        Marshal.FreeCoTaskMem((IntPtr)pFileName.Value);
        return fileName;
    }

    private int GetFileTypeIndex()
    {
        // NOTE: IFileDialog::GetFileTypeIndex method is a one-based
        // index rather than zero-based.
        _fileDialog.GetFileTypeIndex(out var uFileTypeIndex);
        return (int)uFileTypeIndex - 1;
    }

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
    /// File dialog events
    /// </summary>
    private sealed class WindowFileDialogEvents(WindowFileDialog fileDialog) : IFileDialogEvents
    {
        private readonly WindowFileDialog _fileDialog = fileDialog;

        public void OnFileOk(IFileDialog pfd) => Expression.Empty();

        public void OnFolderChanging(IFileDialog pfd, IShellItem psiFolder) => Expression.Empty();

        public void OnFolderChange(IFileDialog pfd) => Expression.Empty();

        public void OnSelectionChange(IFileDialog pfd) => _fileDialog.FileTypeChanged?.Invoke(null, _fileDialog.GetFileType());

        public unsafe void OnShareViolation(IFileDialog pfd, IShellItem psi, FDE_SHAREVIOLATION_RESPONSE* pResponse) => Expression.Empty();

        public void OnTypeChange(IFileDialog pfd) => Expression.Empty();

        public unsafe void OnOverwrite(IFileDialog pfd, IShellItem psi, FDE_OVERWRITE_RESPONSE* pResponse) => Expression.Empty();
    }
}
