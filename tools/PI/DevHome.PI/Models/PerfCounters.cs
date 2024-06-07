// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace DevHome.PI.Models;

public partial class PerfCounters : ObservableObject, IDisposable
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(PerfCounters));

    public static readonly PerfCounters Instance = new();

    private const string ProcessCategory = "Process";
    private const string ProcessorCategory = "Processor Information";
    private const string MemoryCategory = "Memory";
    private const string DiskCategory = "PhysicalDisk";

    private const string CpuCounterName = "% Processor Utility";
    private const string RamCounterName = "Working Set - Private";
    private const string SystemRamCounterName = "Committed Bytes";
    private const string SystemDiskCounterName = "% Disk Time";

    private const string ReadCounterName = "IO Read Bytes/sec";
    private const string WriteCounterName = "IO Write Bytes/sec";
    private const string GpuEngineName = "GPU Engine";
    private const string UtilizationPercentageName = "Utilization Percentage";

    private Process? targetProcess;
    private PerformanceCounter? cpuCounter;
    private List<PerformanceCounter>? gpuCounters;
    private PerformanceCounter? ramCounter;
    private PerformanceCounter? readCounter;
    private PerformanceCounter? writeCounter;

    private PerformanceCounter? systemCpuCounter;
    private PerformanceCounter? systemRamCounter;
    private PerformanceCounter? systemDiskCounter;

    private Timer? timer;

    [ObservableProperty]
    private float cpuUsage;

    [ObservableProperty]
    private float gpuUsage;

    [ObservableProperty]
    private float ramUsageInMB;

    [ObservableProperty]
    private float diskUsage;

    [ObservableProperty]
    private float networkUsage;

    [ObservableProperty]
    private float systemCpuUsage;

    [ObservableProperty]
    private float systemRamUsageInGB;

    [ObservableProperty]
    private float systemDiskUsage;

    public PerfCounters()
    {
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;

        ThreadPool.QueueUserWorkItem((o) =>
        {
            systemCpuCounter = new PerformanceCounter(ProcessorCategory, CpuCounterName, "_Total", true);
            systemRamCounter = new PerformanceCounter(MemoryCategory, SystemRamCounterName, true);
            systemDiskCounter = new PerformanceCounter(DiskCategory, SystemDiskCounterName, "_Total", true);
            UpdateTargetProcess(TargetAppData.Instance.TargetProcess);
        });
    }

    private void TargetApp_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TargetAppData.TargetProcess))
        {
            ThreadPool.QueueUserWorkItem((o) => UpdateTargetProcess(TargetAppData.Instance.TargetProcess));
        }
        else if (e.PropertyName == nameof(TargetAppData.HasExited))
        {
            CloseTargetCounters();
        }
    }

    private void UpdateTargetProcess(Process? process)
    {
        if (process == targetProcess)
        {
            // Already tracking this process.
            return;
        }

        CloseTargetCounters();

        targetProcess = process;
        if (targetProcess == null)
        {
            return;
        }

        var processName = targetProcess.ProcessName;
        cpuCounter = new PerformanceCounter(ProcessCategory, CpuCounterName, processName, true);
        ramCounter = new PerformanceCounter(ProcessCategory, RamCounterName, processName, true);
        gpuCounters = GetGpuCounters(targetProcess.Id);
        readCounter = new PerformanceCounter(ProcessCategory, ReadCounterName, processName, true);
        writeCounter = new PerformanceCounter(ProcessCategory, WriteCounterName, processName, true);
    }

    public void Start()
    {
        Stop();
        timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    public void Stop()
    {
        timer?.Dispose();
        timer = null;
    }

    private void CloseTargetCounters()
    {
        cpuCounter?.Close();
        cpuCounter?.Dispose();
        cpuCounter = null;
        ramCounter?.Close();
        ramCounter?.Dispose();
        ramCounter = null;

        foreach (var counter in gpuCounters ?? Enumerable.Empty<PerformanceCounter>())
        {
            counter.Close();
            counter.Dispose();
        }

        gpuCounters?.Clear();

        readCounter?.Close();
        readCounter?.Dispose();
        readCounter = null;
        writeCounter?.Close();
        writeCounter?.Dispose();
        writeCounter = null;
    }

    public static List<PerformanceCounter> GetGpuCounters(int pid)
    {
        var category = new PerformanceCounterCategory(GpuEngineName);
        var counterNames = category.GetInstanceNames();
        var gpuCounters = counterNames
            .Where(counterName => counterName.Contains($"pid_{pid}"))
            .SelectMany(category.GetCounters)
            .Where(counter => counter.CounterName.Equals(UtilizationPercentageName, StringComparison.Ordinal))
            .ToList();
        return gpuCounters;
    }

    private void TimerCallback(object? state)
    {
        try
        {
            CpuUsage = (cpuCounter?.NextValue() ?? 0) / 100;
            GpuUsage = GetGpuUsage(gpuCounters);

            // Report app memory usage in MB
            RamUsageInMB = ramCounter?.NextValue() / (1024 * 1024) ?? 0;

            var readBytesPerSec = readCounter?.NextValue() ?? 0;
            var writeBytesPerSec = writeCounter?.NextValue() ?? 0;
            var totalDiskBytesPerSec = readBytesPerSec + writeBytesPerSec;
            DiskUsage = totalDiskBytesPerSec / (1024 * 1024);

            SystemCpuUsage = systemCpuCounter?.NextValue() ?? 0;

            // Report system memory usage in GB
            SystemRamUsageInGB = systemRamCounter?.NextValue() / (1024 * 1024 * 1024) ?? 0;
            SystemDiskUsage = systemDiskCounter?.NextValue() ?? 0;
        }
        catch (Exception ex)
        {
            _log.Debug(ex, "Failed to update counters.");
        }
    }

    public static float GetGpuUsage(List<PerformanceCounter>? gpuCounters)
    {
        float result = 0;
        try
        {
            gpuCounters?.ForEach(x => x.NextValue());
            Thread.Sleep(500);
            result = gpuCounters?.Sum(x => x.NextValue()) ?? 0;
        }
        catch (Exception ex)
        {
            _log.Debug(ex, "Failed to get Gpu usage.");
        }

        return result;
    }

    public void Dispose()
    {
        cpuCounter?.Dispose();
        ramCounter?.Dispose();
        readCounter?.Dispose();
        writeCounter?.Dispose();

        foreach (var counter in gpuCounters ?? Enumerable.Empty<PerformanceCounter>())
        {
            counter.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
