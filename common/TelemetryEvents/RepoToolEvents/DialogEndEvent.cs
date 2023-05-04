// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.RepoToolEvents;

[EventData]
public class DialogEndEvent : EventBase
{
    public string DialogName { get; }

    public bool WasDialogCanceled { get; }

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public DialogEndEvent(string dialogName, bool wasCanceled)
    {
        DialogName = dialogName;
        WasDialogCanceled = wasCanceled;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace
    }
}
