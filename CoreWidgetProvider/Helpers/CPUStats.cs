// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

namespace CoreWidgetProvider.Helpers;

internal sealed class CPUStats : IDisposable
{
    // CPU counters
    private readonly PerformanceCounter procPerf = new("Processor Information", "% Processor Utility", "_Total");
    private readonly PerformanceCounter procPerformance = new("Processor Information", "% Processor Performance", "_Total");
    private readonly PerformanceCounter procFrequency = new("Processor Information", "Processor Frequency", "_Total");
    private readonly Dictionary<Process, PerformanceCounter> cpuCounters = new();

    internal sealed class ProcessStats
    {
        public Process? Process { get; set; }

        public float CpuUsage { get; set; }
    }

    public float CpuUsage { get; set; }

    public float CpuSpeed { get; set; }

    public ProcessStats[] ProcessCPUStats { get; set; }

    public List<float> CpuChartValues { get; set; } = new List<float>();

    public CPUStats()
    {
        CpuUsage = 0;
        ProcessCPUStats =
        [
            new ProcessStats(),
            new ProcessStats(),
            new ProcessStats()
        ];

        InitCPUPerfCounters();
    }

    private void InitCPUPerfCounters()
    {
        var allProcesses = Process.GetProcesses().Where(p => (long)p.MainWindowHandle != 0);

        foreach (Process process in allProcesses)
        {
            cpuCounters.Add(process, new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true));
        }
    }

    public void GetData()
    {
        CpuUsage = procPerf.NextValue() / 100;
        CpuSpeed = procFrequency.NextValue() * (procPerformance.NextValue() / 100);

        lock (CpuChartValues)
        {
            ChartHelper.AddNextChartValue(CpuUsage * 100, CpuChartValues);
        }

        var processCPUUsages = new Dictionary<Process, float>();

        foreach (var processCounter in cpuCounters)
        {
            try
            {
                // process might be terminated
                processCPUUsages.Add(processCounter.Key, processCounter.Value.NextValue() / Environment.ProcessorCount);
            }
            catch
            {
            }
        }

        var cpuIndex = 0;
        foreach (var processCPUValue in processCPUUsages.OrderByDescending(x => x.Value).Take(3))
        {
            ProcessCPUStats[cpuIndex].Process = processCPUValue.Key;
            ProcessCPUStats[cpuIndex].CpuUsage = processCPUValue.Value;
            cpuIndex++;
        }
    }

    internal string CreateCPUImageUrl()
    {
        return ChartHelper.CreateImageUrl(CpuChartValues, ChartHelper.ChartType.CPU);
    }

    internal string GetCpuProcessText(int cpuProcessIndex)
    {
        if (cpuProcessIndex >= ProcessCPUStats.Length)
        {
            return "no data";
        }

        return $"{ProcessCPUStats[cpuProcessIndex].Process?.ProcessName} ({ProcessCPUStats[cpuProcessIndex].CpuUsage / 100:p})";
    }

    internal void KillTopProcess(int cpuProcessIndex)
    {
        if (cpuProcessIndex >= ProcessCPUStats.Length)
        {
            return;
        }

        ProcessCPUStats[cpuProcessIndex].Process?.Kill();
    }

    public void Dispose()
    {
        procPerf.Dispose();
        procPerformance.Dispose();
        procFrequency.Dispose();

        foreach (var counter in cpuCounters.Values)
        {
            counter.Dispose();
        }
    }
}
