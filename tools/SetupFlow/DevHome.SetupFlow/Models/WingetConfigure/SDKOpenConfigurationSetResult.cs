// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.SetupFlow.Services;
using SDK = Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models.WingetConfigure;

public class SDKOpenConfigurationSetResult
{
    private readonly ISetupFlowStringResource _setupFlowStringResource;

    public SDKOpenConfigurationSetResult(SDK.OpenConfigurationSetResult result, ISetupFlowStringResource setupFlowStringResource)
    {
        Field = result.Field.Clone() as string;
        Line = result.Line;
        Column = result.Column;
        Value = result.Value.Clone() as string;
        _setupFlowStringResource = setupFlowStringResource;
    }

    // The field that is missing/invalid, if appropriate for the specific ResultCode.
    public string Field { get; }

    // The value of the field, if appropriate for the specific ResultCode.
    public string Value { get; }

    // The line number for the failure reason, if determined.
    public uint Line { get; }

    // The column number for the failure reason, if determined.
    public uint Column { get; }

    public override string ToString()
    {
        return _setupFlowStringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationOpenConfigFailed, Field, Value, Line, Column);
    }
}
