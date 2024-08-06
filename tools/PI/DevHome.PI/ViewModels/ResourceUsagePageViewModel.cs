// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.PI.Helpers;
using DevHome.PI.Models;

namespace DevHome.PI.ViewModels;

public partial class ResourceUsagePageViewModel : ObservableObject, IDisposable
{
    private readonly Timer _timer;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

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

    public ResourceUsagePageViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        PerfCounters.Instance.PropertyChanged += PerfCounterHelper_PropertyChanged;

        // Initial population of values
        _cpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", PerfCounters.Instance.CpuUsage);
        _ramUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormatNoLabel", PerfCounters.Instance.RamUsageInMB);
        _diskUsage = CommonHelper.GetLocalizedString("DiskPerfTextFormatNoLabel", PerfCounters.Instance.DiskUsage);
        _gpuUsage = CommonHelper.GetLocalizedString("GpuPerfTextFormatNoLabel", PerfCounters.Instance.DiskUsage);
        _responding = TargetAppData.Instance.TargetProcess?.Responding ?? false;

        // We don't have a great way to determine when the "Responding" member changes, so we'll poll every 10 seconds using a Timer
        _timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }

    private void TimerCallback(object? state)
    {
        Process? process = TargetAppData.Instance.TargetProcess;

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
                CpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", PerfCounters.Instance.CpuUsage);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.RamUsageInMB))
        {
            _dispatcher.TryEnqueue(() =>
            {
                RamUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormatNoLabel", PerfCounters.Instance.RamUsageInMB);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.DiskUsage))
        {
            _dispatcher.TryEnqueue(() =>
            {
                DiskUsage = CommonHelper.GetLocalizedString("DiskPerfTextFormatNoLabel", PerfCounters.Instance.DiskUsage);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.GpuUsage))
        {
            _dispatcher.TryEnqueue(() =>
            {
                GpuUsage = CommonHelper.GetLocalizedString("GpuPerfTextFormatNoLabel", PerfCounters.Instance.GpuUsage);
            });
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        GC.SuppressFinalize(this);
    }
}
