// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents;

[EventData]
public class DisksAndVolumesSettingsPageTriggeredEvent : EventBase
{
    public DisksAndVolumesSettingsPageTriggeredEvent(string source)
    {
        this.source = source;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }

#pragma warning disable SA1300 // Element should begin with upper-case letter
    public string source { get; }
#pragma warning restore SA1300 // Element should begin with upper-case letter

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;
}
