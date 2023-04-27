// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;
using Microsoft.Management.Configuration;

namespace DevHome.SetupFlow.Common.TelemetryEvents;

[EventData]
internal class ConfigurationUnitResultEvent : EventBase
{
    public ConfigurationUnitResultEvent(ApplyConfigurationUnitResult unitResult)
    {
        _unitResult = unitResult;
    }

    private readonly ApplyConfigurationUnitResult _unitResult;

    public string UnitName => _unitResult.Unit.UnitName;

    public int ExceptionHResult => _unitResult.ResultInformation.ResultCode?.HResult ?? 0;

    public bool RebootRequired => _unitResult.RebootRequired;

    public string FinalState => _unitResult.State.ToString();

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;
}
