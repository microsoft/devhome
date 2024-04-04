// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.Win32.UI.Shell.Common;

namespace DevHome.Common.Windows.FileDialog;

internal sealed class WindowFileDialogFilter : IWindowFileDialogFilter, IDisposable
{
    private readonly COMDLG_FILTERSPEC _extension;
    private bool disposedValue;

    /// <inheritdoc />
    public unsafe string Name { get; }

    /// <inheritdoc />
    public unsafe string Spec { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Patterns { get; }

    internal COMDLG_FILTERSPEC Extension => _extension;

    public unsafe WindowFileDialogFilter(string name, IReadOnlyList<string> patterns)
    {
        if (patterns.Count == 0 || patterns.Any(p => !p.StartsWith(".", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"Pattern list cannot be empty and values should start with a '.'");
        }

        // Patterns
        Patterns = patterns;

        // Name
        Name = name;
        _extension.pszName = (char*)Marshal.StringToHGlobalUni(name);

        // Spec
        var combinedPattern = string.Join(";", patterns.Select(p => $"*{p}"));
        Spec = combinedPattern;
        _extension.pszSpec = (char*)Marshal.StringToHGlobalUni(combinedPattern);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="Dispose()"/>
    private unsafe void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Marshal.FreeHGlobal((IntPtr)_extension.pszName.Value);
                Marshal.FreeHGlobal((IntPtr)_extension.pszSpec.Value);
            }

            disposedValue = true;
        }
    }
}
