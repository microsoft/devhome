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
internal sealed class ConfigurationUnitResultEvent : EventBase
{
    private readonly IDSCApplicationUnitResult _unitResult;

    public string Type => _unitResult.Type;

    public int ExceptionHResult => _unitResult.HResult;

    public string ResultDescription { get; private set; }

    public string ResultDetails { get; private set; }

    public bool RebootRequired => _unitResult.RebootRequired;

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public ConfigurationUnitResultEvent(IDSCApplicationUnitResult unitResult)
    {
        _unitResult = unitResult;
        ResultDescription = _unitResult.ErrorDescription;
        ResultDetails = _unitResult.Details;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        ResultDescription = replaceSensitiveStrings(ResultDescription);
        ResultDetails = replaceSensitiveStrings(ResultDetails);
    }
}
