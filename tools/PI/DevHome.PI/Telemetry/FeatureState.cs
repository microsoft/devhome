// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DevHome.PI.Telemetry;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftWindowsWPF;
using Windows.Foundation.Metadata;
using static DevHome.PI.Telemetry.FeatureState;

namespace DevHome.PI.Telemetry;

// NOTE: The below flags can never be deleted.  We should only rename features that were removed from the product.
// The enum can only take additions over time.
[Flags]
internal enum Feature
{
    None = 0x0,
    AppDetails = 0x1,
    ResourceUsage = 0x2,
    LoadedModule = 0x4,
    WERReports = 0x8,
    WinLogs = 0x10,
    ProcessList = 0x20,
    Insights = 0x40,
    InsightsAtStartup = 0x80,
    MonitorCPU = 0x100,
}

internal enum FeatureShareType
{
    Exclusive,
    Shared,
}

internal sealed class FeatureState
{
    internal static readonly Dictionary<Feature, FeatureShareType> Features = new()
    {
        { Feature.AppDetails, FeatureShareType.Exclusive },
        { Feature.ResourceUsage, FeatureShareType.Exclusive },
        { Feature.LoadedModule, FeatureShareType.Exclusive },
        { Feature.WERReports, FeatureShareType.Exclusive },
        { Feature.WinLogs, FeatureShareType.Exclusive },
        { Feature.ProcessList, FeatureShareType.Exclusive },
        { Feature.Insights, FeatureShareType.Exclusive },
        { Feature.InsightsAtStartup, FeatureShareType.Shared },
        { Feature.MonitorCPU, FeatureShareType.Shared },
    };

    internal static bool IsExclusive(Feature feature)
    {
        if (FeatureState.Features[feature] == FeatureShareType.Exclusive)
        {
            return true;
        }

        return false;
    }
}

public static class EnumExtensions
{
    public static IEnumerable<T> GetFlags<T>(this T en)
        where T : struct, Enum
    {
        return Enum.GetValues<T>().Where(member => en.HasFlag(member)).ToArray();
    }
}
