// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly List<WindowFileDialogFilter> _fileTypes = [];
    private bool disposedValue;

    // File dialog events
    public event EventHandler<IWindowFileDialogFilter?>? FileTypeChanged;

    private protected IFileDialog FileDialog => _fileDialog;

    public WindowFileDialog()
    {
        _fileDialog = CreateInstance();
        _fileDialog.Advise(new WindowFileDialogEvents(this), out _adviseCookie);
        InitializeInstance();
    }

    /// <summary>
    /// Gets or sets the file name that the user has chosen.
    /// </summary>
    /// <returns>File name.</returns>
    public unsafe string FileName
    {
        get
        {
            // Get the file name and free the memory
            _fileDialog.GetFileName(out var pFileName);
            var fileName = new string(pFileName);
            Marshal.FreeCoTaskMem((IntPtr)pFileName.Value);
            return fileName;
        }

        set => _fileDialog.SetFileName(value);
    }

    /// <summary>
    /// Gets the file types that the user can choose from.
    /// </summary>
    /// <returns>List of file types.</returns>
    public IReadOnlyCollection<IWindowFileDialogFilter> AvailableFileTypes => _fileTypes;

    /// <summary>
    /// Add a file type to the file dialog.
    /// </summary>
    /// <param name="displayName">Display name of the file type.</param>
    /// <param name="extensions">File extensions.</param>
    /// <returns>File type.</returns>
    public IWindowFileDialogFilter AddFileType(string displayName, params string[] extensions)
    {
        var filter = new WindowFileDialogFilter(displayName, extensions);
        _fileTypes.Add(filter);
        return filter;
    }

    /// <summary>
    /// Remove a file type from the file dialog.
    /// </summary>
    /// <param name="fileType">File type to remove.</param>
    /// <returns>True if the file type was removed; otherwise, false.</returns>
    public bool RemoveFileType(IWindowFileDialogFilter fileType) => fileType is WindowFileDialogFilter ft && _fileTypes.Remove(ft);

    /// <summary>
    /// Gets the file name that the user has chosen.
    /// </summary>
    /// <returns>File type.</returns>
    public IWindowFileDialogFilter? FileType => _fileTypes.ElementAtOrDefault(GetFileTypeIndex());

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
    protected bool ShowOk(Window window)
    {
        try
        {
            // Set the file types before showing the dialog
            UpdateDialogFileTypes();

            // Show the dialog
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
    /// Add an option to the file dialog.
    /// </summary>
    /// <param name="option">Option to add.</param>
    private protected void AddOption(FILEOPENDIALOGOPTIONS option)
    {
        _fileDialog.GetOptions(out var options);
        options |= option;
        _fileDialog.SetOptions(options);
    }

    /// <summary>
    /// Remove an option from the file dialog.
    /// </summary>
    /// <param name="option">Option to remove.</param>
    private protected void RemoveOption(FILEOPENDIALOGOPTIONS option)
    {
        _fileDialog.GetOptions(out var options);
        options &= ~option;
        _fileDialog.SetOptions(options);
    }

    /// <summary>
    /// Creates an instance of the file dialog.
    /// </summary>
    /// <returns>File dialog instance.</returns>
    private protected abstract IFileDialog CreateInstance();

    /// <summary>
    /// Initializes the file dialog instance after creation.
    /// </summary>
    protected virtual void InitializeInstance()
    {
        // Derived classes can override this method to initialize the desired
        // options for the file dialog
    }

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
    /// Sets the file types that the user can choose from.
    /// </summary>
    private void UpdateDialogFileTypes()
    {
        if (_fileTypes.Count > 0)
        {
            var extensions = _fileTypes.Select(ft => ft.Extension).ToList();
            _fileDialog.SetFileTypes(CollectionsMarshal.AsSpan(extensions));
        }
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

        public void OnTypeChange(IFileDialog pfd) => _fileDialog.FileTypeChanged?.Invoke(null, _fileDialog.FileType);

        /************************************************************
         * Redirect more events here if needed                      *
         ************************************************************/

        public void OnFileOk(IFileDialog pfd)
        {
            // Raise a new event if needed
        }

        public void OnFolderChanging(IFileDialog pfd, IShellItem psiFolder)
        {
            // Raise a new event if needed
        }

        public void OnFolderChange(IFileDialog pfd)
        {
            // Raise a new event if needed
        }

        public void OnSelectionChange(IFileDialog pfd)
        {
            // Raise a new event if needed
        }

        public unsafe void OnShareViolation(IFileDialog pfd, IShellItem psi, FDE_SHAREVIOLATION_RESPONSE* pResponse)
        {
            // Raise a new event if needed
        }

        public unsafe void OnOverwrite(IFileDialog pfd, IShellItem psi, FDE_OVERWRITE_RESPONSE* pResponse)
        {
            // Raise a new event if needed
        }
    }
}
