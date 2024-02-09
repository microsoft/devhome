// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Timer = System.Timers.Timer;

namespace CoreWidgetProvider.Helpers;

internal sealed class DataManager : IDisposable
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
        lock (SystemData.MemStats)
        {
            SystemData.MemStats.GetData();
        }
    }

    private void GetNetworkData()
    {
        lock (SystemData.NetStats)
        {
            SystemData.NetStats.GetData();
        }
    }

    private void GetGPUData()
    {
        lock (SystemData.GPUStats)
        {
            SystemData.GPUStats.GetData();
        }
    }

    private void GetCPUData()
    {
        lock (SystemData.CpuStats)
        {
            SystemData.CpuStats.GetData();
        }
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
        lock (SystemData.MemStats)
        {
            return SystemData.MemStats;
        }
    }

    internal NetworkStats GetNetworkStats()
    {
        lock (SystemData.NetStats)
        {
            return SystemData.NetStats;
        }
    }

    internal GPUStats GetGPUStats()
    {
        lock (SystemData.GPUStats)
        {
            return SystemData.GPUStats;
        }
    }

    internal CPUStats GetCPUStats()
    {
        lock (SystemData.CpuStats)
        {
            return SystemData.CpuStats;
        }
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
