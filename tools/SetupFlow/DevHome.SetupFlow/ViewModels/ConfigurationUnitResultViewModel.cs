// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.Contract.TaskOperator;
using DevHome.SetupFlow.Services;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.ViewModels;

/// <summary>
/// Delegate factory for creating configuration unit result view models
/// </summary>
/// <param name="unitResult">Configuration unit result model</param>
/// <returns>Configuration unit result view model</returns>
public delegate ConfigurationUnitResultViewModel ConfigurationUnitResultViewModelFactory(IConfigurationUnitResult unitResult);

/// <summary>
/// View model for a configuration unit result
/// </summary>
public class ConfigurationUnitResultViewModel
{
    private readonly ISetupFlowStringResource _stringResource;
    private readonly int _hresult;
    private readonly string _intent;
    private readonly string _unitName;

    public ConfigurationUnitResultViewModel(ISetupFlowStringResource stringResource, IConfigurationUnitResult unitResult)
    {
        _stringResource = stringResource;

        // Initialize members in the constructor
        // Note: A unit result can be a proxy for an OOP COM object (e.g. when
        // running elevated). Storing object values in the constructor allows
        // XAML bindings to access the initialized values, without repeatedly
        // querying the COM object.
        _intent = unitResult.Intent;
        _unitName = unitResult.UnitName;
        _hresult = unitResult.HResult;
        IsSkipped = unitResult.IsSkipped;
        IsSuccess = unitResult.HResult == HRESULT.S_OK;
        IsError = !IsSkipped && !IsSuccess;
    }

    public string Title => _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSummary, _intent, _unitName);

    public string ApplyResult => GetApplyResult();

    public bool IsSkipped { get; }

    public bool IsError { get; }

    public bool IsSuccess { get; }

    private string GetApplyResult()
    {
        var hresult = $"0x{_hresult:X}";
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
}
