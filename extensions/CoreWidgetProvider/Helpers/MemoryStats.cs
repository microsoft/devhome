// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace CoreWidgetProvider.Helpers;

internal sealed class MemoryStats : IDisposable
{
    private readonly PerformanceCounter memCommitted = new("Memory", "Committed Bytes", string.Empty);
    private readonly PerformanceCounter memCached = new("Memory", "Cache Bytes", string.Empty);
    private readonly PerformanceCounter memCommittedLimit = new("Memory", "Commit Limit", string.Empty);
    private readonly PerformanceCounter memPoolPaged = new("Memory", "Pool Paged Bytes", string.Empty);
    private readonly PerformanceCounter memPoolNonPaged = new("Memory", "Pool Nonpaged Bytes", string.Empty);

    public float MemUsage
    {
        get; set;
    }

    public ulong AllMem
    {
        get; set;
    }

    public ulong UsedMem
    {
        get; set;
    }

    public ulong MemCommitted
    {
        get; set;
    }

    public ulong MemCommitLimit
    {
        get; set;
    }

    public ulong MemCached
    {
        get; set;
    }

    public ulong MemPagedPool
    {
        get; set;
    }

    public ulong MemNonPagedPool
    {
        get; set;
    }

    public List<float> MemChartValues { get; set; } = new List<float>();

    public void GetData()
    {
        Windows.Win32.System.SystemInformation.MEMORYSTATUSEX memStatus = default;
        memStatus.dwLength = (uint)Marshal.SizeOf(typeof(Windows.Win32.System.SystemInformation.MEMORYSTATUSEX));
        if (PInvoke.GlobalMemoryStatusEx(ref memStatus))
        {
            AllMem = memStatus.ullTotalPhys;
            var availableMem = memStatus.ullAvailPhys;
            UsedMem = AllMem - availableMem;

            MemUsage = (float)UsedMem / AllMem;
            lock (MemChartValues)
            {
                ChartHelper.AddNextChartValue(MemUsage * 100, MemChartValues);
            }
        }

        MemCached = (ulong)memCached.NextValue();
        MemCommitted = (ulong)memCommitted.NextValue();
        MemCommitLimit = (ulong)memCommittedLimit.NextValue();
        MemPagedPool = (ulong)memPoolPaged.NextValue();
        MemNonPagedPool = (ulong)memPoolNonPaged.NextValue();
    }

    public string CreateMemImageUrl()
    {
        return ChartHelper.CreateImageUrl(MemChartValues, ChartHelper.ChartType.Mem);
    }

    public void Dispose()
    {
        memCommitted.Dispose();
        memCached.Dispose();
        memCommittedLimit.Dispose();
        memPoolPaged.Dispose();
        memPoolNonPaged.Dispose();
    }
}
