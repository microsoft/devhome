// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.EnvironmentVariables.TelemetryEvents;

[EventData]
public class EnvironmentVariablesProfileEnabledEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public bool IsEnabled
    {
        get;
    }

    public EnvironmentVariablesProfileEnabledEvent(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
