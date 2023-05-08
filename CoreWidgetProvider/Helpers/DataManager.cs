// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Timer = System.Timers.Timer;

namespace CoreWidgetProvider.Helpers;

internal class DataManager : IDisposable
{
    private readonly SystemData systemData;
    private readonly Action updateAction;

    private readonly Timer updateTimer;

    private const int OneSecondInMilliseconds = 1000;

    public DataManager(Action updateWidget)
    {
        systemData = new SystemData();

        updateAction = updateWidget;

        updateTimer = new Timer(OneSecondInMilliseconds);
        updateTimer.Elapsed += UpdateTimer_Elapsed;
        updateTimer.AutoReset = true;
        updateTimer.Enabled = false;
    }

    private void GetMemoryData()
    {
        systemData.MemStats.GetData();
    }

    private void GetNetworkData()
    {
        systemData.NetStats.GetData();
    }

    private void GetCPUData()
    {
        systemData.CpuStats.GetData();
    }

    private void UpdateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        lock (systemData)
        {
            // memory
            GetMemoryData();

            // network
            GetNetworkData();

            // CPU
            GetCPUData();
        }

        updateAction();
    }

    internal MemoryStats GetMemoryStats()
    {
        return systemData.MemStats;
    }

    internal NetworkStats GetNetworkStats()
    {
        return systemData.NetStats;
    }

    internal CPUStats GetCPUStats()
    {
        return systemData.CpuStats;
    }

    public void Start()
    {
        updateTimer.Start();
    }

    public void Stop()
    {
        updateTimer.Stop();
    }

    public void Dispose()
    {
        systemData.Dispose();
        updateTimer.Dispose();
    }
}
