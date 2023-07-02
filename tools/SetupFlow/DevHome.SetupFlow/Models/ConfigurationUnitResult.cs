// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.Contract.TaskOperator;
using Microsoft.Management.Configuration;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.Models;
public class ConfigurationUnitResult
{
    public ConfigurationUnitResult(ApplyConfigurationUnitResult result)
    {
        UnitName = result.Unit.UnitName;
        Intent = result.Unit.Intent.ToString();
        IsSkipped = result.State == ConfigurationUnitState.Skipped;
        HResult = result.ResultInformation?.ResultCode?.HResult ?? HRESULT.S_OK;
    }

    public ConfigurationUnitResult(IConfigurationUnitResult result)
    {
        UnitName = result.UnitName;
        Intent = result.Intent;
        IsSkipped = result.IsSkipped;
        HResult = result.HResult;
    }

    public string UnitName { get; }

    public string Intent { get; }

    public bool IsSkipped { get; }

    public int HResult { get; }
}
