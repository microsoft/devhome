// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;

namespace CoreWidgetProvider.Helpers;
internal class DiskStats : IDisposable
{
    private readonly Dictionary<string, List<PerformanceCounter>> diskCounters = new ();

    private Dictionary<string, Data> DiskUsages { get; set; } = new Dictionary<string, Data>();

    private Dictionary<string, List<float>> DiskChartValues { get; set; } = new Dictionary<string, List<float>>();

    public class Data
    {
        public float Usage
        {
            get; set;
        }

        public ulong ReadBytesPerSecond
        {
            get; set;
        }

        public ulong WriteBytesPerSecond
        {
            get; set;
        }
    }

    public DiskStats()
    {
        InitDiskPerfCounters();
    }

    private void InitDiskPerfCounters()
    {
        PerformanceCounterCategory pcc = new PerformanceCounterCategory("PhysicalDisk");
        var instanceNames = pcc.GetInstanceNames();
        foreach (var instanceName in instanceNames)
        {
            if (instanceName == "_Total")
            {
                continue;
            }

            List<PerformanceCounter> instanceCounters = new List<PerformanceCounter>();
            instanceCounters.Add(new PerformanceCounter("PhysicalDisk", "% Disk Time", instanceName));
            instanceCounters.Add(new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", instanceName));
            instanceCounters.Add(new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", instanceName));
            diskCounters.Add(instanceName, instanceCounters);
            DiskChartValues.Add(instanceName, new List<float>());
            DiskUsages.Add(instanceName, new Data());
        }
    }

    public void GetData()
    {
        foreach (var diskCounter in diskCounters)
        {
            try
            {
                var usage = diskCounter.Value[0].NextValue();
                var readBytesPerSecond = diskCounter.Value[1].NextValue();
                var writeBytesPerSecond = diskCounter.Value[2].NextValue();

                var name = diskCounter.Key;
                DiskUsages[name].Usage = usage;
                DiskUsages[name].ReadBytesPerSecond = (ulong)readBytesPerSecond;
                DiskUsages[name].WriteBytesPerSecond = (ulong)writeBytesPerSecond;

                List<float> chartValues = DiskChartValues[name];
                ChartHelper.AddNextChartValue(usage, chartValues);
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError("Error getting disk data.", ex);
            }
        }
    }

    public string GetDiskName(int diskIndex)
    {
        if (DiskChartValues.Count <= diskIndex)
        {
            return string.Empty;
        }

        return DiskChartValues.ElementAt(diskIndex).Key;
    }

    public Data GetDiskData(int diskIndex)
    {
        if (DiskChartValues.Count <= diskIndex)
        {
            return new Data();
        }

        var currDiskName = DiskChartValues.ElementAt(diskIndex).Key;
        if (!DiskUsages.ContainsKey(currDiskName))
        {
            return new Data();
        }

        return DiskUsages[currDiskName];
    }

    public string CreateDiskImageUrl(int diskChartIndex)
    {
        return ChartHelper.CreateImageUrl(DiskChartValues.ElementAt(diskChartIndex).Value, "disk");
    }

    public void Dispose()
    {
        foreach (var counterPair in diskCounters)
        {
            foreach (var counter in counterPair.Value)
            {
                counter.Dispose();
            }
        }
    }
}
