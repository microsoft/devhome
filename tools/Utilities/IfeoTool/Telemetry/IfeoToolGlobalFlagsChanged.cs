// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.IfeoTool;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.IfeoTool.TelemetryEvents;

[EventData]
public class IfeoToolGlobalFlagsChanged : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public IfeoFlags GlobalFlags
    {
        get; private set;
    }

    public IfeoToolGlobalFlagsChanged(IfeoFlags flags)
    {
        GlobalFlags = flags;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
