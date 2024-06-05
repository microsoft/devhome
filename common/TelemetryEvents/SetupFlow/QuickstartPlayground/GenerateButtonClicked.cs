// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.SetupFlow.QuickstartPlayground;

[EventData]
public class GenerateButtonClicked(string prompt) : EventBase
{
    public string Prompt { get; } = prompt;

    public override PartA_PrivTags PartA_PrivTags => PartA_PrivTags.ProductAndServiceUsage;

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
