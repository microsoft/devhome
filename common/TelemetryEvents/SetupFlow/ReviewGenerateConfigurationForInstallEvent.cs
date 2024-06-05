// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.SetupFlow;

[EventData]
public class ReviewGenerateConfigurationForInstallEvent : EventBase
{
    /// <summary>
    /// Gets the number installed packages generated as part of the
    /// configuration file
    /// </summary>
    public int InstalledCount { get; }

    /// <summary>
    /// Gets the number non-installed packages generated as part of the
    /// configuration file
    /// </summary>
    public int NotInstalledCount { get; }

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public ReviewGenerateConfigurationForInstallEvent(int installedCount, int notInstalledCount)
    {
        InstalledCount = installedCount;
        NotInstalledCount = notInstalledCount;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
