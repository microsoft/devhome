// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.TelemetryEvents;

[EventData]
public class AppInstallEvent : EventBase
{
    public string PackageId { get; }

    public string SourceId { get; }

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public AppInstallEvent(string packageId, string sourceId)
    {
        PackageId = packageId;
        SourceId = sourceId;
    }
}
