// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.DevHomeDatabase;

[EventData]
public class DatabaseMigrationErrorEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PartA_PrivTags.ProductAndServicePerformance;

    public uint PreviousSchemaVersion { get; set; }

    public uint CurrentSchemaVersion { get; set; }

    public int HResult { get; }

    public string ExceptionMessage { get; } = string.Empty;

    public DatabaseMigrationErrorEvent(Exception ex, uint previousSchemaVersion, uint currentSchemaVersion)
    {
        HResult = ex.HResult;
        ExceptionMessage = ex.Message;
        PreviousSchemaVersion = previousSchemaVersion;
        CurrentSchemaVersion = currentSchemaVersion;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
