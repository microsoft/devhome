// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.TelemetryEvents.SetupFlow;

[EventData]
public class DialogEvent : EventBase
{
    public string Action { get; }

    public string DialogName { get; }

    public ContentDialogResult DialogResult { get; }

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public DialogEvent(string action, string dialogName, ContentDialogResult dialogResult = ContentDialogResult.None)
    {
        Action = action;
        DialogName = dialogName;
        DialogResult = dialogResult;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
