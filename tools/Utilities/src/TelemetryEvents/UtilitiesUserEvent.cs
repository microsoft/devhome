// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Common.Helpers;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Utilities.TelemetryEvents;

[EventData]
public class UtilitiesUserEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public string UtilityName
    {
        get;
    }

    public bool LaunchedAsAdmin
    {
        get;
    }

    public UtilitiesUserEvent(string utilityName, bool launchedAsAdmin)
    {
        UtilityName = utilityName;
        LaunchedAsAdmin = launchedAsAdmin;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
