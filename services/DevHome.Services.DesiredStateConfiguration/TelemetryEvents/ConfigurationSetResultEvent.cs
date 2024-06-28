// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Services.DesiredStateConfiguration.Contracts;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Services.DesiredStateConfiguration.Services;

[EventData]
internal sealed class ConfigurationSetResultEvent : EventBase
{
    private readonly IDSCApplicationResult _setResult;

    public string SetName => _setResult.AppliedSet.Name;

    public string SetInstanceIdentifier => _setResult.AppliedSet.InstanceIdentifier.ToString();

    public int ExceptionHResult => _setResult.ResultException?.HResult ?? 0;

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public ConfigurationSetResultEvent(IDSCApplicationResult setResult)
    {
        _setResult = setResult;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
