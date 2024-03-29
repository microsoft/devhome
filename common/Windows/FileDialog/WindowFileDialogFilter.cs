// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.Win32.UI.Shell.Common;

namespace DevHome.Common.Windows.FileDialog;

public class WindowFileDialogFilter : IDisposable
{
    private readonly COMDLG_FILTERSPEC _extension;
    private bool disposedValue;

    public unsafe string? Name => Marshal.PtrToStringUni((IntPtr)_extension.pszName.Value);

    public unsafe string? Spec => Marshal.PtrToStringUni((IntPtr)_extension.pszSpec.Value);

    public IReadOnlyList<string> Patterns { get; }

    internal COMDLG_FILTERSPEC Extension => _extension;

    public unsafe WindowFileDialogFilter(string name, List<string> patterns)
    {
        if (patterns.Count == 0 || patterns.Any(p => !p.StartsWith(".", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"Pattern list cannot be empty and values should start with a '.'");
        }

        Patterns = patterns;
        _extension.pszName = (char*)Marshal.StringToHGlobalUni(name);
        var combinedPattern = string.Join(";", patterns.Select(p => $"*{p}"));
        _extension.pszSpec = (char*)Marshal.StringToHGlobalUni(combinedPattern);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

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
