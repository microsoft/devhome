// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.SetupFlow.Common.TelemetryEvents;

[EventData]
internal sealed class DevDriveCreationEvent : EventBase
{
    public DevDriveCreationEvent()
    {
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;
}
