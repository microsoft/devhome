// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.DevHomeDatabase;

[EventData]
public class DevHomeDatabaseEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PartA_PrivTags.ProductAndServicePerformance;

    public string Step { get; } = string.Empty;

    public int HResult { get; }

    public string ExceptionMessage { get; } = string.Empty;

    public DevHomeDatabaseEvent(string step)
    {
        Step = step;
    }

    public DevHomeDatabaseEvent(string step, Exception ex)
    {
        Step = step;
        HResult = ex.HResult;
        ExceptionMessage = ex.Message;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
