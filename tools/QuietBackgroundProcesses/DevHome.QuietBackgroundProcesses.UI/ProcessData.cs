// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Drawing;
using System.Numerics;
using Windows.UI;

namespace DevHome.QuietBackgroundProcesses.UI;

public class ProcessData
{
    public enum ProcessCategory
    {
        Unknown,
        User,
        System,
        Developer,
        Background,
    }

    public ProcessData()
    {
        Name = string.Empty;
        PackageFullName = string.Empty;
        Aumid = string.Empty;
        Path = string.Empty;
    }

    public long Pid { get; set; }

    public string Name { get; set; }

    public string PackageFullName { get; set; }

    public string Aumid { get; set; }

    public string Path { get; set; }

    public ProcessCategory Category { get; set; }

    public DateTimeOffset CreateTime { get; set; }

    public DateTimeOffset ExitTime { get; set; }

    public ulong Samples { get; set; }

    public float Percent { get; set; }

    public float StandardDeviation { get; set; }

    public float Sigma4Deviation { get; set; }

    public float MaxPercent { get; set; }

    public TimeSpan TimeAboveThreshold { get; set; }

    public double TimeAboveThresholdInMinutes { get; set; }

    public ulong TotalCpuTimeInMicroseconds { get; set; }
}
