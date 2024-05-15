// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.PI.Helpers;
using DevHome.PI.Models;

namespace DevHome.PI.ViewModels;

public partial class BarWindowViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue dispatcher;

    [ObservableProperty]
    private string systemCpuUsage = string.Empty;

    [ObservableProperty]
    private string systemRamUsage = string.Empty;

    [ObservableProperty]
    private string systemDiskUsage = string.Empty;

    public BarWindowViewModel()
    {
        dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        PerfCounters.Instance.PropertyChanged += PerfCounterHelper_PropertyChanged;

        SystemCpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", PerfCounters.Instance.SystemCpuUsage);
        SystemRamUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormatNoLabelGB", PerfCounters.Instance.SystemRamUsageInGB);
        SystemDiskUsage = CommonHelper.GetLocalizedString("DiskPerfPercentUsageTextFormatNoLabel", PerfCounters.Instance.SystemDiskUsage);
    }

    private void PerfCounterHelper_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PerfCounters.SystemCpuUsage))
        {
            dispatcher.TryEnqueue(() =>
            {
                SystemCpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", PerfCounters.Instance.SystemCpuUsage);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.SystemRamUsageInGB))
        {
            dispatcher.TryEnqueue(() =>
            {
                // Convert from bytes to GBs
                SystemRamUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormatNoLabelGB", PerfCounters.Instance.SystemRamUsageInGB);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.SystemDiskUsage))
        {
            dispatcher.TryEnqueue(() =>
            {
                SystemDiskUsage = CommonHelper.GetLocalizedString("DiskPerfPercentUsageTextFormatNoLabel", PerfCounters.Instance.SystemDiskUsage);
            });
        }
    }
}
