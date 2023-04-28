// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;
using Microsoft.Management.Configuration;

namespace DevHome.SetupFlow.Common.TelemetryEvents;

[EventData]
internal class ConfigurationUnitResultEvent : EventBase
{
    private readonly ApplyConfigurationUnitResult _unitResult;

    public string UnitName => _unitResult.Unit.UnitName;

    public int ExceptionHResult => _unitResult.ResultInformation.ResultCode?.HResult ?? 0;

    public string ResultDescription { get; private set; }

    public string ResultDetails { get; private set; }

    public bool RebootRequired => _unitResult.RebootRequired;

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public ConfigurationUnitResultEvent(ApplyConfigurationUnitResult unitResult)
    {
        _unitResult = unitResult;
        ResultDescription = _unitResult.ResultInformation.Description;
        ResultDetails = _unitResult.ResultInformation.Details;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        ResultDescription = replaceSensitiveStrings(ResultDescription);
        ResultDetails = replaceSensitiveStrings(ResultDetails);
    }
}
