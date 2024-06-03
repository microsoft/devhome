// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.SetupFlow;

[EventData]
public class EndFlowEvent : EventBase
{
    public string CallerName
    {
        get;
    }

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public EndFlowEvent(string callerName)
    {
        CallerName = callerName;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
