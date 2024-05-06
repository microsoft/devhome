// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.SetupFlow;

[EventData]
public class ReviewGenerateConfigurationCommandEvent : EventBase
{
    public List<string> Flow { get; } = [];

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public ReviewGenerateConfigurationCommandEvent(List<string> flow)
    {
        Flow = flow;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
