// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents;

[EventData]
public class NavigationViewItemEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public string NavigationViewTitle
    {
        get;
    }

    public string PreviousViewTitle
    {
        get;
    }

    public NavigationViewItemEvent(string navigationViewTitle, string previousViewTitle)
    {
        NavigationViewTitle = navigationViewTitle;
        PreviousViewTitle = previousViewTitle;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // Only storing HRESULT. No sensitive information.
    }
}
