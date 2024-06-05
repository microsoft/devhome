// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;
using Microsoft.Management.Configuration;

namespace DevHome.Services.WindowsPackageManager.TelemetryEvents;

[EventData]
internal sealed class ConfigurationUnitResultEvent : EventBase
{
    private readonly ApplyConfigurationUnitResult _unitResult;

    public string Type => _unitResult.Unit.Type;

    public int ExceptionHResult => _unitResult.ResultInformation.ResultCode?.HResult ?? 0;

    public string ResultDescription { get; private set; }

    public string ResultDetails { get; private set; }

    public bool RebootRequired => _unitResult.RebootRequired;

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

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
