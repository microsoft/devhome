// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.SetupFlow.Common.TelemetryEvents;

[EventData]
internal class DisksAndVolumesSettingsPageTriggeredEvent : EventBase
{
    public DisksAndVolumesSettingsPageTriggeredEvent(string source)
    {
        this.source = source;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
    }

#pragma warning disable SA1300 // Element should begin with upper-case letter
    public string source { get; }
#pragma warning restore SA1300 // Element should begin with upper-case letter

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;
}
