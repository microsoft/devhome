// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace CoreWidgetProvider.Helpers;

internal class SystemData : IDisposable
{
    public static MemoryStats MemStats { get; set; } = new MemoryStats();

    public static NetworkStats NetStats { get; set; } = new NetworkStats();

    public static GPUStats GPUStats { get; set; } = new GPUStats();

    public static CPUStats CpuStats { get; set; } = new CPUStats();

    public static DiskStats DiskStats { get; set; } = new DiskStats();

    public SystemData()
    {
    }

    public void Dispose()
    {
    }
}
