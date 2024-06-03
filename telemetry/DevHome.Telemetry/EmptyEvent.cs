// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Telemetry;

[EventData]
public class EmptyEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags { get; }

    public EmptyEvent(PartA_PrivTags tags)
    {
        PartA_PrivTags = tags;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive string
    }
}
