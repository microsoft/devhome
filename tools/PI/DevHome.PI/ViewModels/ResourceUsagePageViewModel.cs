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
    private readonly Timer timer;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue dispatcher;

    [ObservableProperty]
    private string cpuUsage = string.Empty;

    [ObservableProperty]
    private string ramUsage = string.Empty;

    [ObservableProperty]
    private string diskUsage = string.Empty;

    [ObservableProperty]
    private string gpuUsage = string.Empty;

    [ObservableProperty]
    private bool responding;

    public ResourceUsagePageViewModel()
    {
        dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        PerfCounters.Instance.PropertyChanged += PerfCounterHelper_PropertyChanged;

        // Initial population of values
        cpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", PerfCounters.Instance.CpuUsage);
        ramUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormatNoLabel", PerfCounters.Instance.RamUsageInMB);
        diskUsage = CommonHelper.GetLocalizedString("DiskPerfTextFormatNoLabel", PerfCounters.Instance.DiskUsage);
        gpuUsage = CommonHelper.GetLocalizedString("GpuPerfTextFormatNoLabel", PerfCounters.Instance.DiskUsage);
        responding = TargetAppData.Instance.TargetProcess?.Responding ?? false;

        // We don't have a great way to determine when the "Responding" member changes, so we'll poll every 10 seconds using a Timer
        timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }

    private void TimerCallback(object? state)
    {
        Process? process = TargetAppData.Instance.TargetProcess;

        if (process is not null)
        {
            var newResponding = process.Responding;

            if (newResponding != Responding)
            {
                dispatcher.TryEnqueue(() =>
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
            dispatcher.TryEnqueue(() =>
            {
                CpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", PerfCounters.Instance.CpuUsage);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.RamUsageInMB))
        {
            dispatcher.TryEnqueue(() =>
            {
                RamUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormatNoLabel", PerfCounters.Instance.RamUsageInMB);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.DiskUsage))
        {
            dispatcher.TryEnqueue(() =>
            {
                DiskUsage = CommonHelper.GetLocalizedString("DiskPerfTextFormatNoLabel", PerfCounters.Instance.DiskUsage);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.GpuUsage))
        {
            dispatcher.TryEnqueue(() =>
            {
                GpuUsage = CommonHelper.GetLocalizedString("GpuPerfTextFormatNoLabel", PerfCounters.Instance.GpuUsage);
            });
        }
    }

    public void Dispose()
    {
        timer.Dispose();
        GC.SuppressFinalize(this);
    }
}
