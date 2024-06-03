// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.PI.TelemetryEvents;

[EventData]
public class LogStateChangedEventData : EventBase
{
    internal LogStateChangedEventData(string stateName, string stateValue)
    {
        StateName = stateName;
        StateValue = stateValue;
    }

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public string StateName { get; private set; }

    public string StateValue { get; private set; }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
    }
}
