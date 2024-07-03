// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Services.WindowsPackageManager.TelemetryEvents;

[EventData]
internal sealed class AppInstallUserEvent : EventBase
{
    public string PackageId { get; }

    public string SourceId { get; }

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public AppInstallUserEvent(string packageId, string sourceId)
    {
        PackageId = packageId;
        SourceId = sourceId;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
