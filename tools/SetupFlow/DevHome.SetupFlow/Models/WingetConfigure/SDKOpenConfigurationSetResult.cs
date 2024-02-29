// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Services;
using SDK = Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models.WingetConfigure;

public class SDKOpenConfigurationSetResult
{
    private readonly ISetupFlowStringResource _setupFlowStringResource;

    public SDKOpenConfigurationSetResult(SDK.OpenConfigurationSetResult result, ISetupFlowStringResource setupFlowStringResource)
    {
        Result = result;
        _setupFlowStringResource = setupFlowStringResource;

        if (Result != null)
        {
            Field = new(result.Field);
            Line = result.Line;
            Column = result.Column;
            Value = new(result.Value);
            ResultCode = Result.ResultCode;
            Succeeded = ResultCode == null || ResultCode?.HResult == 0;
        }
    }

    public bool Succeeded { get; private set; }

    public SDK.OpenConfigurationSetResult Result { get; private set; }

    public Exception ResultCode { get; private set; }

    // The field that is missing/invalid, if appropriate for the specific ResultCode.
    public string Field { get; } = string.Empty;

    // The value of the field, if appropriate for the specific ResultCode.
    public string Value { get; } = string.Empty;

    // The line number for the failure reason, if determined.
    public uint Line { get; }

    // The column number for the failure reason, if determined.
    public uint Column { get; }

    public string GetErrorMessage()
    {
        Log.Logger?.ReportError(
            Log.Component.SDKOpenConfigurationSetResult,
            $"Extension failed to open the configuration file provided by Dev Home: Field: {Field}, Value: {Value}, Line: {Line}, Column: {Column}",
            ResultCode);

        return _setupFlowStringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationOpenConfigFailed);
    }
}
