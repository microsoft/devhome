// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.SetupFlow;

[EventData]
public class ExtensionEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public int NumberOfProviders
    {
        get;
    }

    public ExtensionEvent(int numberOfProviders)
    {
        NumberOfProviders = numberOfProviders;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.s
    }
}
