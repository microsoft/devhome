// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

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

    public NavigationViewItemEvent(string navigationViewTitle)
    {
        NavigationViewTitle = navigationViewTitle;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // Only storing Hresult.  No sensitive information.
    }
}
