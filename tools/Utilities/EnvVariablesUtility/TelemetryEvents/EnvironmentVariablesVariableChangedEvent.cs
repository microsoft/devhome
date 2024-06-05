// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using EnvironmentVariablesUILib.Models;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.EnvironmentVariables.TelemetryEvents;

[EventData]
public class EnvironmentVariablesVariableChangedEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public VariablesSetType VariableType
    {
        get;
    }

    public EnvironmentVariablesVariableChangedEvent(VariablesSetType variableType)
    {
        VariableType = variableType;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
