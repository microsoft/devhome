// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;
using Windows.Win32;
using Timer = System.Timers.Timer;

namespace CoreWidgetProvider.Helpers;

public class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (IHardware subHardware in hardware.SubHardware)
        {
            subHardware.Accept(this);
        }
    }

    public void VisitSensor(ISensor sensor)
    {
    }

    public void VisitParameter(IParameter parameter)
    {
    }
}

internal class DataManager : IDisposable
{
    private readonly SystemData systemData;
    private readonly Action updateAction;

    // memory counters
    private readonly PerformanceCounter memCommitted = new ("Memory", "Committed Bytes", string.Empty);
    private readonly PerformanceCounter memCached = new ("Memory", "Cache Bytes", string.Empty);
    private readonly PerformanceCounter memCommittedLimit = new ("Memory", "Commit Limit", string.Empty);
    private readonly PerformanceCounter memPoolPaged = new ("Memory", "Pool Paged Bytes", string.Empty);
    private readonly PerformanceCounter memPoolNonPaged = new ("Memory", "Pool Nonpaged Bytes", string.Empty);

    private readonly Timer updateTimer;

    private const int MaxChartValues = 30;

    private const int OneSecondInMilliseconds = 1000;

    public DataManager(Action updateWidget)
    {
        systemData = new SystemData();

        updateAction = updateWidget;

        updateTimer = new Timer(OneSecondInMilliseconds);
        updateTimer.Elapsed += UpdateTimer_Elapsed;
        updateTimer.AutoReset = true;
        updateTimer.Enabled = true;
    }

    public void GetMemoryData()
    {
        Windows.Win32.System.SystemInformation.MEMORYSTATUSEX memStatus = new ();
        memStatus.dwLength = (uint)Marshal.SizeOf(typeof(Windows.Win32.System.SystemInformation.MEMORYSTATUSEX));
        if (PInvoke.GlobalMemoryStatusEx(out memStatus))
        {
            systemData.MemStats.AllMem = memStatus.ullTotalPhys;
            var availableMem = memStatus.ullAvailPhys;
            systemData.MemStats.UsedMem = systemData.MemStats.AllMem - availableMem;

            systemData.MemStats.MemUsage = (float)systemData.MemStats.UsedMem / systemData.MemStats.AllMem;
            AddNextChartValue(systemData.MemStats.MemUsage * 100, systemData.MemStats.MemChartValues);
        }

        systemData.MemStats.MemCached = (ulong)memCached.NextValue();
        systemData.MemStats.MemCommited = (ulong)memCommitted.NextValue();
        systemData.MemStats.MemCommitLimit = (ulong)memCommittedLimit.NextValue();
        systemData.MemStats.MemPagedPool = (ulong)memPoolPaged.NextValue();
        systemData.MemStats.MemNonPagedPool = (ulong)memPoolNonPaged.NextValue();
    }

    private void UpdateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        lock (systemData)
        {
            // memory
            GetMemoryData();
        }

        updateAction();
    }

    private void AddNextChartValue(float value, List<float> chartValues)
    {
        if (chartValues.Count >= MaxChartValues)
        {
            chartValues.RemoveAt(0);
        }

        chartValues.Add(value);
    }

    internal MemoryStats GetMemoryStats()
    {
        return systemData.MemStats;
    }

    internal string CreateMemImageUrl()
    {
        return ChartHelper.CreateImageUrl(systemData.MemStats.MemChartValues);
    }

    public void Dispose()
    {
        updateTimer.Dispose();
    }
}
