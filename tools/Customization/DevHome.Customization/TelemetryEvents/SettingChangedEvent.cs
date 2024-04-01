// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Customization.TelemetryEvents;

[EventData]
public class SettingChangedEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public string Id
    {
        get;
    }

    public string Value
    {
        get;
    }

    public SettingChangedEvent(string id, string value)
    {
        Id = id;
        Value = value;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }

    public static void Log(string settingName, string value)
    {
        TelemetryFactory.Get<ITelemetry>().Log("Customization_SettingChanged_Event", LogLevel.Measure, new SettingChangedEvent(settingName, value));
    }
}
