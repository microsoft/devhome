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

namespace DevHome.DevInsights.Models;

public partial class PerfCounters : ObservableObject, IDisposable
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(PerfCounters));

    public static readonly PerfCounters Instance = new();

    private const string ProcessCategory = "Process";
    private const string ProcessorCategory = "Processor Information";
    private const string MemoryCategory = "Memory";
    private const string DiskCategory = "PhysicalDisk";

    private const string CpuCounterName = "% Processor Time";
    private const string RamCounterName = "Working Set - Private";
    private const string SystemCpuCounterName = "% Processor Utility";
    private const string SystemRamCounterName = "Committed Bytes";
    private const string SystemDiskCounterName = "% Disk Time";

    private const string ReadCounterName = "IO Read Bytes/sec";
    private const string WriteCounterName = "IO Write Bytes/sec";
    private const string GpuEngineName = "GPU Engine";
    private const string UtilizationPercentageName = "Utilization Percentage";

    private Process? _targetProcess;
    private PerformanceCounter? _cpuCounter;
    private List<PerformanceCounter>? _gpuCounters;
    private PerformanceCounter? _ramCounter;
    private PerformanceCounter? _readCounter;
    private PerformanceCounter? _writeCounter;

    private PerformanceCounter? _systemCpuCounter;
    private PerformanceCounter? _systemRamCounter;
    private PerformanceCounter? _systemDiskCounter;

    private Timer? _timer;

    [ObservableProperty]
    private float _cpuUsage;

    [ObservableProperty]
    private float _gpuUsage;

    [ObservableProperty]
    private float _ramUsageInMB;

    [ObservableProperty]
    private float _diskUsage;

    [ObservableProperty]
    private float _networkUsage;

    [ObservableProperty]
    private float _systemCpuUsage;

    [ObservableProperty]
    private float _systemRamUsageInGB;

    [ObservableProperty]
    private float _systemDiskUsage;

    public PerfCounters()
    {
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;

        ThreadPool.QueueUserWorkItem((o) =>
        {
            _systemCpuCounter = new PerformanceCounter(ProcessorCategory, SystemCpuCounterName, "_Total", true);
            _systemRamCounter = new PerformanceCounter(MemoryCategory, SystemRamCounterName, true);
            _systemDiskCounter = new PerformanceCounter(DiskCategory, SystemDiskCounterName, "_Total", true);
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
        if (process == _targetProcess)
        {
            // Already tracking this process.
            return;
        }

        CloseTargetCounters();

        _targetProcess = process;
        if (_targetProcess == null)
        {
            return;
        }

        var processName = _targetProcess.ProcessName;
        _cpuCounter = new PerformanceCounter(ProcessCategory, CpuCounterName, processName, true);
        _ramCounter = new PerformanceCounter(ProcessCategory, RamCounterName, processName, true);
        _gpuCounters = GetGpuCounters(_targetProcess.Id);
        _readCounter = new PerformanceCounter(ProcessCategory, ReadCounterName, processName, true);
        _writeCounter = new PerformanceCounter(ProcessCategory, WriteCounterName, processName, true);
    }

    public void Start()
    {
        Stop();
        _timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void CloseTargetCounters()
    {
        _cpuCounter?.Close();
        _cpuCounter?.Dispose();
        _cpuCounter = null;
        _ramCounter?.Close();
        _ramCounter?.Dispose();
        _ramCounter = null;

        foreach (var counter in _gpuCounters ?? Enumerable.Empty<PerformanceCounter>())
        {
            counter.Close();
            counter.Dispose();
        }

        _gpuCounters?.Clear();

        _readCounter?.Close();
        _readCounter?.Dispose();
        _readCounter = null;
        _writeCounter?.Close();
        _writeCounter?.Dispose();
        _writeCounter = null;
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
            CpuUsage = _cpuCounter?.NextValue() / Environment.ProcessorCount ?? 0;
            GpuUsage = GetGpuUsage(_gpuCounters);

            // Report app memory usage in MB
            RamUsageInMB = _ramCounter?.NextValue() / (1024 * 1024) ?? 0;

            var readBytesPerSec = _readCounter?.NextValue() ?? 0;
            var writeBytesPerSec = _writeCounter?.NextValue() ?? 0;
            var totalDiskBytesPerSec = readBytesPerSec + writeBytesPerSec;
            DiskUsage = totalDiskBytesPerSec / (1024 * 1024);

            SystemCpuUsage = _systemCpuCounter?.NextValue() ?? 0;

            // Report system memory usage in GB
            SystemRamUsageInGB = _systemRamCounter?.NextValue() / (1024 * 1024 * 1024) ?? 0;
            SystemDiskUsage = _systemDiskCounter?.NextValue() ?? 0;
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
        _cpuCounter?.Dispose();
        _ramCounter?.Dispose();
        _readCounter?.Dispose();
        _writeCounter?.Dispose();

        foreach (var counter in _gpuCounters ?? Enumerable.Empty<PerformanceCounter>())
        {
            counter.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
