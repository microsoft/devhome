// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Globalization;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.ViewModels;

/// <summary>
/// Delegate factory for creating configuration unit result view models
/// </summary>
/// <param name="unitResult">Configuration unit result model</param>
/// <returns>Configuration unit result view model</returns>
public delegate ConfigurationUnitResultViewModel ConfigurationUnitResultViewModelFactory(ConfigurationUnitResult unitResult);

/// <summary>
/// View model for a configuration unit result
/// </summary>
public class ConfigurationUnitResultViewModel
{
    private readonly ConfigurationUnitResult _unitResult;
    private readonly ISetupFlowStringResource _stringResource;

    public ConfigurationUnitResultViewModel(ISetupFlowStringResource stringResource, ConfigurationUnitResult unitResult)
    {
        _stringResource = stringResource;
        _unitResult = unitResult;
    }

    public string Title => BuildTitle();

    public string ApplyResult => GetApplyResult();

    public bool IsSkipped => _unitResult.IsSkipped;

    public bool IsError => !IsSkipped && _unitResult.HResult != HRESULT.S_OK;

    public bool IsSuccess => _unitResult.HResult == HRESULT.S_OK;

    private string GetApplyResult()
    {
        var hresult = $"0x{_unitResult.HResult:X}";
        if (IsSkipped)
        {
            return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSkipped, hresult);
        }

        if (IsError)
        {
            return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitFailed, hresult);
        }

        return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSuccess);
    }

    private string BuildTitle()
    {
        if (string.IsNullOrEmpty(_unitResult.Id) && string.IsNullOrEmpty(_unitResult.Description))
        {
            return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSummaryMinimal, _unitResult.Intent, _unitResult.UnitName);
        }

        if (string.IsNullOrEmpty(_unitResult.Id))
        {
            return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSummaryNoId, _unitResult.Intent, _unitResult.UnitName, _unitResult.Description);
        }

        if (string.IsNullOrEmpty(_unitResult.Description))
        {
            return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSummaryNoDescription, _unitResult.Intent, _unitResult.UnitName, _unitResult.Id);
        }

        return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSummaryFull, _unitResult.Intent, _unitResult.UnitName, _unitResult.Id, _unitResult.Description);
    }
}
