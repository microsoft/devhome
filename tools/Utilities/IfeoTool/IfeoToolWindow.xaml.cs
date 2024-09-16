// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using DevHome.Common.Services;
using DevHome.Common.Windows;
using DevHome.Telemetry;

namespace DevHome.IfeoTool;

public sealed partial class IfeoToolWindow : ThemeAwareWindow, IDisposable
{
    private readonly ImageOptionsControlViewModel _viewModel = new(IfeoToolApp.TargetAppName);
    private bool _disposed;

    internal static string GetLocalizedString(string stringName, params object[] args)
    {
        var stringResource = new StringResource();
        var localizedString = stringResource.GetLocalized(stringName, args);
        Debug.Assert(!string.IsNullOrEmpty(localizedString), stringName + " is empty. Check if " + stringName + " is present in Resources.resw.");
        return localizedString;
    }

    public IfeoToolWindow()
    {
        InitializeComponent();

        var title = GetLocalizedString("IfeoToolWindowTitle");
        IfeoToolTitle.Text = $"{title}{IfeoToolApp.TargetAppName}";
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
