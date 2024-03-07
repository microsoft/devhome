// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CoreWidgetProvider.Helpers;

internal sealed class SystemData : IDisposable
{
    public static MemoryStats MemStats { get; set; } = new MemoryStats();

    public static NetworkStats NetStats { get; set; } = new NetworkStats();

    public static GPUStats GPUStats { get; set; } = new GPUStats();

    public static CPUStats CpuStats { get; set; } = new CPUStats();

    public SystemData()
    {
    }

    public void Dispose()
    {
    }
}
