// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Telemetry;

namespace DevHome.IfeoTool;

public sealed partial class IfeoToolWindow : WinUIEx.WindowEx, IDisposable
{
    private readonly ImageOptionsControlViewModel _viewModel = new(IfeoToolApp.TargetAppName);
    private bool _disposed;

    public IfeoToolWindow()
    {
        InitializeComponent();

        // TODO: Sync the theme changes...

        // TODO: Set the titlebar text to the target app name + " Image File Execution Options"
        Title = IfeoToolApp.TargetAppName;
        IfeoToolApp.Log("IfeoToolApp_IfeoToolWindow_Initialized", LogLevel.Measure);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _viewModel.Dispose();
            }

            _disposed = true;
        }
    }
}
