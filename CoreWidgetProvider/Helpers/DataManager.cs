// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Timer = System.Timers.Timer;

namespace CoreWidgetProvider.Helpers;

internal class DataManager : IDisposable
{
    private readonly SystemData systemData;
    private readonly DataType dataType;
    private readonly Timer updateTimer;
    private readonly Action updateAction;

    private const int OneSecondInMilliseconds = 1000;

    public DataManager(DataType type, Action updateWidget)
    {
        systemData = new SystemData();
        updateAction = updateWidget;
        dataType = type;

        updateTimer = new Timer(OneSecondInMilliseconds);
        updateTimer.Elapsed += UpdateTimer_Elapsed;
        updateTimer.AutoReset = true;
        updateTimer.Enabled = false;
    }

    private void GetMemoryData()
    {
        SystemData.MemStats.GetData();
    }

    private void GetNetworkData()
    {
        SystemData.NetStats.GetData();
    }

    private void GetGPUData()
    {
        SystemData.GPUStats.GetData();
    }

    private void GetCPUData()
    {
        SystemData.CpuStats.GetData();
    }

    private void UpdateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        switch (dataType)
        {
            case DataType.CPU:
                {
                    // CPU
                    GetCPUData();
                    break;
                }

            case DataType.GPU:
                {
                    // gpu
                    GetGPUData();
                    break;
                }

            case DataType.Memory:
                {
                    // memory
                    GetMemoryData();
                    break;
                }

            case DataType.Network:
                {
                    // network
                    GetNetworkData();
                    break;
                }
        }

        if (updateAction != null)
        {
            updateAction();
        }
    }

    internal MemoryStats GetMemoryStats()
    {
        return SystemData.MemStats;
    }

    internal NetworkStats GetNetworkStats()
    {
        return SystemData.NetStats;
    }

    internal GPUStats GetGPUStats()
    {
        return SystemData.GPUStats;
    }

    internal CPUStats GetCPUStats()
    {
        return SystemData.CpuStats;
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
