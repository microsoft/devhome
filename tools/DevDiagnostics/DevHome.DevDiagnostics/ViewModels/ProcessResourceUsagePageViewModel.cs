// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.DevDiagnostics.Helpers;
using DevHome.DevDiagnostics.Models;
using DevHome.DevDiagnostics.Services;
using Microsoft.Diagnostics.Tracing.AutomatedAnalysis;
using Microsoft.UI.Xaml;

namespace DevHome.DevDiagnostics.ViewModels;

public partial class ProcessResourceUsagePageViewModel : ObservableObject, IDisposable
{
    private readonly Timer _respondingTimer;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
    private readonly PerfCounters _perfCounters;

    [ObservableProperty]
    private string _cpuUsage = string.Empty;

    [ObservableProperty]
    private string _ramUsage = string.Empty;

    [ObservableProperty]
    private string _diskUsage = string.Empty;

    [ObservableProperty]
    private string _gpuUsage = string.Empty;

    [ObservableProperty]
    private bool _responding;

    public ProcessResourceUsagePageViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _perfCounters = Application.Current.GetService<PerfCounters>();
        _perfCounters.PropertyChanged += PerfCounterHelper_PropertyChanged;

        // Initial population of values
        _cpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", _perfCounters.CpuUsage);
        _ramUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormatNoLabel", _perfCounters.RamUsageInMB);
        _diskUsage = CommonHelper.GetLocalizedString("DiskPerfTextFormatNoLabel", _perfCounters.DiskUsage);
        _gpuUsage = CommonHelper.GetLocalizedString("GpuPerfTextFormatNoLabel", _perfCounters.GpuUsage);
        _responding = TargetAppData.Instance.TargetProcess?.Responding ?? false;

        // We don't have a great way to determine when the "Responding" member changes, so we'll poll every 10 seconds using a Timer
        _respondingTimer = new Timer(RespondingTimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }

    private void RespondingTimerCallback(object? state)
    {
        System.Diagnostics.Process? process = TargetAppData.Instance.TargetProcess;

        if (process is not null)
        {
            var newResponding = process.Responding;

            if (newResponding != Responding)
            {
                _dispatcher.TryEnqueue(() =>
                {
                    Responding = newResponding;
                });
            }
        }
    }

    private void PerfCounterHelper_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PerfCounters.CpuUsage))
        {
            _dispatcher.TryEnqueue(() =>
            {
                CpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", _perfCounters.CpuUsage);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.RamUsageInMB))
        {
            _dispatcher.TryEnqueue(() =>
            {
                RamUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormatNoLabel", _perfCounters.RamUsageInMB);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.DiskUsage))
        {
            _dispatcher.TryEnqueue(() =>
            {
                DiskUsage = CommonHelper.GetLocalizedString("DiskPerfTextFormatNoLabel", _perfCounters.DiskUsage);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.GpuUsage))
        {
            _dispatcher.TryEnqueue(() =>
            {
                GpuUsage = CommonHelper.GetLocalizedString("GpuPerfTextFormatNoLabel", _perfCounters.GpuUsage);
            });
        }
    }

    public void Dispose()
    {
        _respondingTimer.Dispose();
        GC.SuppressFinalize(this);
    }
}
