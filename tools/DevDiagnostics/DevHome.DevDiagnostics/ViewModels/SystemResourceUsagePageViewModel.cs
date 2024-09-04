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

public partial class SystemResourceUsagePageViewModel : ObservableObject, IDisposable
{
    private readonly Timer _hardwareMonitorTimer;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
    private readonly PerfCounters _perfCounters;
    [ObservableProperty]
    private HardwareMonitor _hardwareMonitor;

    [ObservableProperty]
    private string _cpuUsage = string.Empty;

    [ObservableProperty]
    private string _ramUsage = string.Empty;

    [ObservableProperty]
    private string _diskUsage = string.Empty;

    public SystemResourceUsagePageViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _perfCounters = Application.Current.GetService<PerfCounters>();
        _perfCounters.PropertyChanged += PerfCounterHelper_PropertyChanged;
        _hardwareMonitor = Application.Current.GetService<HardwareMonitor>();

        // Initial population of values
        _cpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", _perfCounters.SystemCpuUsage);
        _ramUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormatNoLabel", _perfCounters.SystemRamUsageInGB);
        _diskUsage = CommonHelper.GetLocalizedString("DiskPerfPercentUsageTextFormatNoLabel", _perfCounters.SystemDiskUsage);

        _hardwareMonitorTimer = new Timer(HardwareMonitorTimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    }

    private void HardwareMonitorTimerCallback(object? state)
    {
        _dispatcher.TryEnqueue(() =>
        {
            HardwareMonitor.UpdateHardwares();
        });
    }

    private void PerfCounterHelper_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PerfCounters.SystemCpuUsage))
        {
            _dispatcher.TryEnqueue(() =>
            {
                CpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", _perfCounters.SystemCpuUsage);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.SystemRamUsageInGB))
        {
            _dispatcher.TryEnqueue(() =>
            {
                RamUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormatNoLabelGB", _perfCounters.SystemRamUsageInGB);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.SystemDiskUsage))
        {
            _dispatcher.TryEnqueue(() =>
            {
                DiskUsage = CommonHelper.GetLocalizedString("DiskPerfPercentUsageTextFormatNoLabel", _perfCounters.SystemDiskUsage);
            });
        }
    }

    public void Dispose()
    {
        _hardwareMonitorTimer.Dispose();
        GC.SuppressFinalize(this);
    }
}
