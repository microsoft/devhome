// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.SetupFlow;

[EventData]
public class ConfigureCommandEvent : EventBase
{
    public bool IsRunningAsAdmin
    {
        get;
    }

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public ConfigureCommandEvent(bool isRunningAsAdmin)
    {
        IsRunningAsAdmin = isRunningAsAdmin;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
