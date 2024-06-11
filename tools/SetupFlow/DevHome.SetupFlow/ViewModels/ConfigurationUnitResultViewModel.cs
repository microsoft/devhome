// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Services.DesiredStateConfiguration.Exceptions;
using DevHome.Services.WindowsPackageManager.Exceptions;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using Microsoft.Management.Configuration;
using Microsoft.UI.Xaml.Media.Imaging;
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
        SetName();
    }

    public string Title => BuildTitle();

    public string ApplyResult => GetApplyResult();

    public string ErrorDescription => GetErrorDescription();

    public bool IsSkipped => _unitResult.IsSkipped;

    public bool IsError => !IsSkipped && _unitResult.HResult != HRESULT.S_OK;

    public bool IsSuccess => _unitResult.HResult == HRESULT.S_OK;

    public bool IsCloneRepoUnit => string.Equals(_unitResult.Type, "GitClone", System.StringComparison.OrdinalIgnoreCase);

    public bool IsPrivateRepo => false;

    public string Name { get; private set; }

    public BitmapImage StatusSymbolIcon { get; set; }

    private void SetName()
    {
        // Get either Git repo name for if this is a repo clone request or package name for package install request.
        // We add into Id in ConfigurationFileBuilder.
        if (!string.IsNullOrEmpty(_unitResult.Id))
        {
            if (IsCloneRepoUnit)
            {
                var start = ConfigurationFileBuilder.RepoNamePrefix.Length;
                var descriptionParts = _unitResult.Id.Substring(start).Split(ConfigurationFileBuilder.RepoNameSuffix);
                Name = descriptionParts[0];
            }
            else
            {
                var descriptionParts = _unitResult.Id.Split(ConfigurationFileBuilder.PackageNameSeparator);

                if (descriptionParts.Length > 1)
                {
                    Name = descriptionParts[1];
                }
                else
                {
                    Name = descriptionParts[0];
                }
            }
        }
        else if (!string.IsNullOrEmpty(_unitResult.UnitDescription))
        {
            Name = _unitResult.UnitDescription;
        }
        else
        {
            Name = "<No Data>";
        }
    }

    private string GetApplyResult()
    {
        if (IsSkipped)
        {
            return GetUnitSkipMessage();
        }

        if (IsError)
        {
            return GetUnitErrorMessage();
        }

        return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSuccess);
    }

    private string BuildTitle()
    {
        if (string.IsNullOrEmpty(_unitResult.Id) && string.IsNullOrEmpty(_unitResult.UnitDescription))
        {
            return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSummaryMinimal, _unitResult.Intent, _unitResult.Type);
        }

        if (string.IsNullOrEmpty(_unitResult.Id))
        {
            return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSummaryNoId, _unitResult.Intent, _unitResult.Type, _unitResult.UnitDescription);
        }

        if (string.IsNullOrEmpty(_unitResult.UnitDescription))
        {
            return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSummaryNoDescription, _unitResult.Intent, _unitResult.Type, _unitResult.Id);
        }

        return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSummaryFull, _unitResult.Intent, _unitResult.Type, _unitResult.Id, _unitResult.UnitDescription);
    }

    private string GetUnitSkipMessage()
    {
        var resultCode = _unitResult.HResult;
        switch (resultCode)
        {
            case WinGetConfigurationException.WingetConfigErrorManuallySkipped:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitManuallySkipped);
            case WinGetConfigurationException.WingetConfigErrorDependencyUnsatisfied:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitNotRunDueToDependency);
            case WinGetConfigurationException.WingetConfigErrorAssertionFailed:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitNotRunDueToFailedAssert);
        }

        var resultCodeHex = $"0x{resultCode:X}";
        return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSkipped, resultCodeHex);
    }

    private string GetUnitErrorMessage()
    {
        var resultCode = _unitResult.HResult;
        switch (resultCode)
        {
            case WinGetConfigurationException.WingetConfigErrorDuplicateIdentifier:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitHasDuplicateIdentifier, _unitResult.Id);
            case WinGetConfigurationException.WingetConfigErrorMissingDependency:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitHasMissingDependency, _unitResult.Details);
            case WinGetConfigurationException.WingetConfigErrorAssertionFailed:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitAssertHadNegativeResult);
            case WinGetConfigurationException.WinGetConfigUnitNotFound:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitNotFoundInModule);
            case WinGetConfigurationException.WinGetConfigUnitNotFoundRepository:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitNotFound);
            case WinGetConfigurationException.WinGetConfigUnitMultipleMatches:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitMultipleMatches);
            case WinGetConfigurationException.WinGetConfigUnitInvokeGet:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitFailedDuringGet);
            case WinGetConfigurationException.WinGetConfigUnitInvokeTest:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitFailedDuringTest);
            case WinGetConfigurationException.WinGetConfigUnitInvokeSet:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitFailedDuringSet);
            case WinGetConfigurationException.WinGetConfigUnitModuleConflict:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitModuleConflict);
            case WinGetConfigurationException.WinGetConfigUnitImportModule:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitModuleImportFailed);
            case WinGetConfigurationException.WinGetConfigUnitInvokeInvalidResult:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitReturnedInvalidResult);
            case WinGetConfigurationException.WingetConfigErrorManuallySkipped:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitManuallySkipped);
            case WinGetConfigurationException.WingetConfigErrorDependencyUnsatisfied:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitNotRunDueToDependency);
            case WinGetConfigurationException.WinGetConfigUnitSettingConfigRoot:
                return _stringResource.GetLocalized(StringResourceKey.WinGetConfigUnitSettingConfigRoot);
            case WinGetConfigurationException.WinGetConfigUnitImportModuleAdmin:
                return _stringResource.GetLocalized(StringResourceKey.WinGetConfigUnitImportModuleAdmin);
        }

        var resultCodeHex = $"0x{resultCode:X}";
        switch (_unitResult.ResultSource)
        {
            case ConfigurationUnitResultSource.ConfigurationSet:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitFailedConfigSet, resultCodeHex);
            case ConfigurationUnitResultSource.Internal:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitFailedInternal, resultCodeHex);
            case ConfigurationUnitResultSource.Precondition:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitFailedPrecondition, resultCodeHex);
            case ConfigurationUnitResultSource.SystemState:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitFailedSystemState, resultCodeHex);
            case ConfigurationUnitResultSource.UnitProcessing:
                return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitFailedUnitProcessing, resultCodeHex);
        }

        return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitFailed, resultCodeHex);
    }

    private string GetErrorDescription()
    {
        if (string.IsNullOrEmpty(_unitResult.ErrorDescription))
        {
            return string.Empty;
        }

        // If the localized configuration error message requires additional
        // context, display the error description from the resource module directly.
        // Code reference: https://github.com/microsoft/winget-cli/blob/master/src/AppInstallerCLICore/Workflows/ConfigurationFlow.cpp
        switch (_unitResult.HResult)
        {
            case WinGetConfigurationException.WingetConfigErrorDuplicateIdentifier:
            case WinGetConfigurationException.WingetConfigErrorMissingDependency:
            case WinGetConfigurationException.WingetConfigErrorAssertionFailed:
            case WinGetConfigurationException.WinGetConfigUnitNotFound:
            case WinGetConfigurationException.WinGetConfigUnitNotFoundRepository:
            case WinGetConfigurationException.WinGetConfigUnitMultipleMatches:
            case WinGetConfigurationException.WinGetConfigUnitModuleConflict:
            case WinGetConfigurationException.WinGetConfigUnitImportModule:
            case WinGetConfigurationException.WinGetConfigUnitInvokeInvalidResult:
            case WinGetConfigurationException.WinGetConfigUnitSettingConfigRoot:
            case WinGetConfigurationException.WinGetConfigUnitImportModuleAdmin:
                return string.Empty;
            default:
                return _unitResult.ErrorDescription;
        }
    }
}
