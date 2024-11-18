// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.RepositoryManagement;

[EventData]
public class RepositoryDataAccessEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PartA_PrivTags.ProductAndServicePerformance;

    public string Action { get; } = string.Empty;

    public string Message { get; } = string.Empty;

    public RepositoryDataAccessEvent(string action, string message)
    {
        Action = action;
        Message = message;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensative strings to replace
    }
}
