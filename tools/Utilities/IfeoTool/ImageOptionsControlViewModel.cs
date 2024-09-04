// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace DevHome.IfeoTool;

public partial class ImageOptionsControlViewModel : ObservableObject, IDisposable
{
    private readonly DispatcherQueue _dispatcher;
    private readonly string _imageName;

    private ImageFileExecutionOptions? _ifeo;
    private bool _disposed;

    [ObservableProperty]
    private bool _isAvrfEnabled = false;

    public ImageOptionsControlViewModel(string imageName)
    {
        _imageName = imageName;
        _dispatcher = DispatcherQueue.GetForCurrentThread();

        InitializeTargetAppState();

        PopulateViewModelProperties();
    }

    private void InitializeTargetAppState()
    {
        // Create a new Ifeo object and subscribe to the GlobalFlagsChanged event.
        _ifeo = new ImageFileExecutionOptions(_imageName);
        _ifeo.GlobalFlagsChanged += OnGlobalFlagsChanged;
    }

    private void OnGlobalFlagsChanged()
    {
        // Make sure to update the properties on the UI thread.
        _dispatcher.TryEnqueue(PopulateViewModelProperties);
    }

    private void PopulateViewModelProperties()
    {
        if (_ifeo == null)
        {
            return;
        }

        IsAvrfEnabled = _ifeo.GlobalFlags.HasFlag(IfeoFlags.ApplicationVerifier);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (_ifeo == null)
        {
            return;
        }

        if (e?.PropertyName == nameof(IsAvrfEnabled))
        {
            if (IsAvrfEnabled)
            {
                _ifeo.GlobalFlags |= IfeoFlags.ApplicationVerifier;
            }
            else
            {
                _ifeo.GlobalFlags &= ~IfeoFlags.ApplicationVerifier;
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                if (_ifeo != null)
                {
                    _ifeo.GlobalFlagsChanged -= OnGlobalFlagsChanged;
                    _ifeo.Dispose();
                }
            }

            _disposed = true;
        }
    }
}
