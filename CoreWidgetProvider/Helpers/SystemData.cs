// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace CoreWidgetProvider.Helpers;

internal class SystemData : IDisposable
{
    public MemoryStats MemStats { get; set; }

    public NetworkStats NetStats { get; set; }

    public GPUStats GPUStats { get; set; }

    public CPUStats CpuStats { get; set; }

    public SystemData()
    {
        MemStats = new MemoryStats();
        NetStats = new NetworkStats();
        GPUStats = new GPUStats();
        CpuStats = new CPUStats();
    }

    public void Dispose()
    {
        MemStats.Dispose();
        NetStats.Dispose();
        GPUStats.Dispose();
        CpuStats.Dispose();
    }
}
