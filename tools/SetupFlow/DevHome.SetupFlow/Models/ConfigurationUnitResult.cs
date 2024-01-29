// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

extern alias Projection;

using Microsoft.Management.Configuration;
using Projection::DevHome.SetupFlow.ElevatedComponent.Helpers;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.Models;

public class ConfigurationUnitResult
{
    public ConfigurationUnitResult(ApplyConfigurationUnitResult result)
    {
        UnitName = result.Unit.UnitName;
        Id = result.Unit.Identifier;
        result.Unit.Directives.TryGetValue("description", out var descriptionObj);
        Description = descriptionObj?.ToString() ?? string.Empty;
        Intent = result.Unit.Intent.ToString();
        IsSkipped = result.State == ConfigurationUnitState.Skipped;
        HResult = result.ResultInformation?.ResultCode?.HResult ?? HRESULT.S_OK;
        ResultSource = result.ResultInformation?.ResultSource ?? ConfigurationUnitResultSource.None;
        Details = result.ResultInformation?.Details;
    }

    public ConfigurationUnitResult(ElevatedConfigureUnitTaskResult result)
    {
        UnitName = result.UnitName;
        Id = result.Id;
        Description = result.Description;
        Intent = result.Intent;
        IsSkipped = result.IsSkipped;
        HResult = result.HResult;
        ResultSource = (ConfigurationUnitResultSource)result.ResultSource;
        Details = result.Details;
    }

    public string UnitName { get; }

    public string Id { get; }

    public string Description { get; }

    public string Intent { get; }

    public bool IsSkipped { get; }

    public int HResult { get; }

    public ConfigurationUnitResultSource ResultSource { get; }

    public string Details { get; }
}
