// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

extern alias Projection;

using DevHome.Services.DesiredStateConfiguration.Contracts;
using Microsoft.Management.Configuration;
using Projection::DevHome.SetupFlow.ElevatedComponent.Helpers;
using Windows.Win32.Foundation;

using SDK = Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models;

public class ConfigurationUnitResult
{
    public ConfigurationUnitResult(IDSCApplicationUnitResult result)
    {
        Type = result.AppliedUnit.Type;
        Id = result.AppliedUnit.Id;
        UnitDescription = result.AppliedUnit.Description;
        Intent = result.AppliedUnit.Intent;
        IsSkipped = result.IsSkipped;
        HResult = result.HResult;
        ResultSource = result.ResultSource;
        Details = result.Details;
        ErrorDescription = result.ErrorDescription;
    }

    public ConfigurationUnitResult(ElevatedConfigureUnitTaskResult result)
    {
        Type = result.Type;
        Id = result.Id;
        UnitDescription = result.UnitDescription;
        Intent = result.Intent;
        IsSkipped = result.IsSkipped;
        HResult = result.HResult;
        ResultSource = (ConfigurationUnitResultSource)result.ResultSource;
        Details = result.Details;
        ErrorDescription = result.ErrorDescription;
    }

    public ConfigurationUnitResult(SDK.ApplyConfigurationUnitResult result)
    {
        Type = result.Unit.Type;
        Id = result.Unit.Identifier;
        if (result.Unit.Settings?.TryGetValue("description", out var descriptionObj) == true)
        {
            UnitDescription = descriptionObj?.ToString() ?? string.Empty;
        }

        Intent = result.Unit.Intent.ToString();
        IsSkipped = result.State == SDK.ConfigurationUnitState.Skipped;
        HResult = result.ResultInformation?.ResultCode?.HResult ?? HRESULT.S_OK;
        SdkResultSource = result.ResultInformation?.ResultSource ?? SDK.ConfigurationUnitResultSource.None;
        Details = result.ResultInformation?.Details;
        ErrorDescription = result.ResultInformation?.Description;
    }

    public string Type { get; }

    public string Id { get; }

    public string UnitDescription { get; }

    public string ErrorDescription { get; }

    public string Intent { get; }

    public bool IsSkipped { get; }

    public int HResult { get; }

    public ConfigurationUnitResultSource ResultSource { get; }

    public SDK.ConfigurationUnitResultSource SdkResultSource { get; }

    public string Details { get; }
}
