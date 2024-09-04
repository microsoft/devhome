// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.DevDiagnostics.Models;
using Microsoft.UI.Xaml;

namespace DevHome.DevDiagnostics.ViewModels;

public partial class ResourceUsagePageViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    [ObservableProperty]
    private Visibility _perAppDataVisibility;

    public ResourceUsagePageViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _perAppDataVisibility = TargetAppData.Instance.TargetProcess is null ? Visibility.Collapsed : Visibility.Visible;
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;
    }

    private void TargetApp_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TargetAppData.TargetProcess))
        {
            var process = TargetAppData.Instance.TargetProcess;
            _dispatcher.TryEnqueue(() =>
            {
                // The App status bar is only visibile if we're attached to a process
                PerAppDataVisibility = process is null ? Visibility.Collapsed : Visibility.Visible;
            });
        }
    }
}
