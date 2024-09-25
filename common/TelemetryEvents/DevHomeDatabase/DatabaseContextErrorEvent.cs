// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.DevHomeDatabase;

[EventData]
public class DatabaseContextErrorEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PartA_PrivTags.ProductAndServicePerformance;

    public string Action { get; } = string.Empty;

    public int HResult { get; }

    public string ExceptionMessage { get; } = string.Empty;

    public DatabaseContextErrorEvent(string action)
    {
        Action = action;
    }

    public DatabaseContextErrorEvent(string action, Exception ex)
    {
        Action = action;
        HResult = ex.HResult;
        ExceptionMessage = ex.Message;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // no sensitive strings to replace.
    }
}
