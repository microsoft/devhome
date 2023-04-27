// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;
using Microsoft.Management.Configuration;

namespace DevHome.SetupFlow.Common.TelemetryEvents;

[EventData]
internal class ConfigurationSetResultEvent : EventBase
{
    public ConfigurationSetResultEvent(ConfigurationSet configSet, ApplyConfigurationSetResult setResult)
    {
        _configSet = configSet;
        _setResult = setResult;
    }

    private readonly ConfigurationSet _configSet;

    private readonly ApplyConfigurationSetResult _setResult;

    public int ExceptionHResult => _setResult.ResultCode?.HResult ?? 0;

    public string SetName => _configSet.Name;

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;
}
