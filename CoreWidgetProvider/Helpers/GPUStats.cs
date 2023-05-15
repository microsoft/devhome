// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Management;
using LibreHardwareMonitor.Hardware;

namespace CoreWidgetProvider.Helpers;

internal class GPUStats : IDisposable
{
    // GPU counters
    private readonly Dictionary<int, List<PerformanceCounter>> gpuCounters = new ();

    private readonly List<Data> stats = new ();

    public class Data
    {
        public string? Name { get; set; }

        public int PhysId { get; set; }

        public float Usage { get; set; }

        public float Temperature { get; set; }

        public List<float> GpuChartValues { get; set; } = new List<float>();
    }

    public GPUStats()
    {
        AddGPUPerfCounters();
    }

    public void AddGPUPerfCounters()
    {
        using (var searcher = new ManagementObjectSearcher("select * from Win32_VideoController"))
        {
            var i = 0;
            foreach (ManagementObject obj in searcher.Get())
            {
                var gpuName = (string)obj["Name"];
                stats.Add(new Data() { Name = gpuName, PhysId = i++ });
            }
        }

        var pcg = new PerformanceCounterCategory("GPU Engine");
        var instanceNames = pcg.GetInstanceNames();

        foreach (var instanceName in instanceNames)
        {
            if (!instanceName.EndsWith("3D", StringComparison.InvariantCulture))
            {
                continue;
            }

            foreach (var counter in pcg.GetCounters(instanceName).Where(x => x.CounterName.StartsWith("Utilization Percentage", StringComparison.InvariantCulture)))
            {
                var counterKey = counter.InstanceName;

                // skip these values
                GetKeyValueFromCounterKey("pid", ref counterKey);
                GetKeyValueFromCounterKey("luid", ref counterKey);

                int phys;
                var success = int.TryParse(GetKeyValueFromCounterKey("phys", ref counterKey), out phys);
                if (success)
                {
                    GetKeyValueFromCounterKey("eng", ref counterKey);
                    var engtype = GetKeyValueFromCounterKey("engtype", ref counterKey);
                    if (engtype != "3D")
                    {
                        continue;
                    }

                    if (!gpuCounters.ContainsKey(phys))
                    {
                        gpuCounters.Add(phys, new ());
                    }

                    gpuCounters[phys].Add(counter);
                }
            }
        }
    }

    public void GetData()
    {
        try
        {
            foreach (var gpu in stats)
            {
                List<PerformanceCounter>? counters;
                var success = gpuCounters.TryGetValue(gpu.PhysId, out counters);

                if (success)
                {
                    var sum = counters?.Sum(x => x.NextValue());
                    gpu.Usage = sum.GetValueOrDefault(0) / 100;
                    ChartHelper.AddNextChartValue(sum.GetValueOrDefault(0), gpu.GpuChartValues);
                }
            }

            Computer computer = new Computer
            {
                IsCpuEnabled = false,
                IsGpuEnabled = true,
                IsMemoryEnabled = false,
                IsMotherboardEnabled = false,
                IsControllerEnabled = false,
                IsNetworkEnabled = false,
                IsStorageEnabled = false,
            };

            computer.Open();
            computer.Accept(new UpdateVisitor());

            // Put in a separate for loop as computer.Open() can throw so this way at least gpu.Usage is obtained.
            // Number of GPUs is usually not big, so performance wise is not that consuming.
            foreach (var gpu in stats)
            {
                IHardware? hardware = computer.Hardware.Where(x => x.Name.Equals(gpu.Name, StringComparison.Ordinal)).FirstOrDefault();

                if (hardware != null)
                {
                    var sensorValue = hardware.Sensors[0].Value.GetValueOrDefault();
                    gpu.Temperature = sensorValue;
                }
            }

            computer.Close();
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError($"Getting GPU data failed: {ex}");
        }
    }

    internal string CreateGPUImageUrl(int gpuChartIndex)
    {
        return ChartHelper.CreateImageUrl(stats.ElementAt(gpuChartIndex).GpuChartValues);
    }

    internal string GetGPUName(int gpuActiveIndex)
    {
        if (stats.Count <= gpuActiveIndex)
        {
            return string.Empty;
        }

        return stats[gpuActiveIndex].Name ?? string.Empty;
    }

    internal int GetPrevGPUIndex(int gpuActiveIndex)
    {
        if (stats.Count == 0)
        {
            return 0;
        }

        if (gpuActiveIndex == 0)
        {
            return stats.Count - 1;
        }

        return gpuActiveIndex - 1;
    }

    internal int GetNextGPUIndex(int gpuActiveIndex)
    {
        if (stats.Count == 0)
        {
            return 0;
        }

        if (gpuActiveIndex == stats.Count - 1)
        {
            return 0;
        }

        return gpuActiveIndex + 1;
    }

    internal float GetGPUUsage(int gpuActiveIndex, string gpuActiveEngType)
    {
        if (stats.Count <= gpuActiveIndex)
        {
            return 0;
        }

        return stats[gpuActiveIndex].Usage;
    }

    internal string GetGPUTemperature(int gpuActiveIndex)
    {
        if (stats.Count <= gpuActiveIndex)
        {
            return "--";
        }

        var temperature = stats[gpuActiveIndex].Temperature;
        if (temperature == 0)
        {
            return "--";
        }

        return temperature.ToString("0.", CultureInfo.InvariantCulture) + " \x00B0C";
    }

    private string GetKeyValueFromCounterKey(string key, ref string counterKey)
    {
        if (!counterKey.StartsWith(key, StringComparison.InvariantCulture))
        {
            // throw new Exception();
            return "error";
        }

        counterKey = counterKey.Substring(key.Length + 1);
        if (key.Equals("engtype", StringComparison.Ordinal))
        {
            return counterKey;
        }

        var pos = counterKey.IndexOf('_');
        if (key.Equals("luid", StringComparison.Ordinal))
        {
            pos = counterKey.IndexOf('_', pos + 1);
        }

        var retValue = counterKey.Substring(0, pos);
        counterKey = counterKey.Substring(pos + 1);
        return retValue;
    }

    public void Dispose()
    {
        foreach (var counterPair in gpuCounters)
        {
            foreach (var counter in counterPair.Value)
            {
                counter.Dispose();
            }
        }
    }
}
