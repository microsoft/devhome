// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;
using Windows.Foundation.Diagnostics;

namespace DevHome.QuietBackgroundProcesses.UI;

[EventData]
public class QuietBackgroundProcessesEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public LoggingOpcode Opcode { get; }

    public QuietBackgroundProcessesEvent(LoggingOpcode opcode = LoggingOpcode.Info)
    {
        Opcode = opcode;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
    }
}
