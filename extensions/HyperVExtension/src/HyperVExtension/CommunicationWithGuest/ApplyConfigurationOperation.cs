// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.HostGuestCommunication;
using HyperVExtension.Models;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Win32.Foundation;
using SDK = Microsoft.Windows.DevHome.SDK;

namespace HyperVExtension.CommunicationWithGuest;

public sealed partial class ApplyConfigurationOperation : IApplyConfigurationOperation, IDisposable
{
    private readonly HyperVVirtualMachine _virtualMachine;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private bool _disposed;

    public string Configuration { get; private set; } = string.Empty;

    public ApplyConfigurationOperation(HyperVVirtualMachine virtualMachine, string configuration)
    {
        _virtualMachine = virtualMachine;
        Configuration = configuration;
    }

    public ApplyConfigurationOperation(HyperVVirtualMachine virtualMachine, Exception result, string? resultDescription = null)
    {
        _virtualMachine = virtualMachine;
        CompletionStatus = new SDK.ApplyConfigurationResult(result, result.Message, result.Message);
    }

    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    public event TypedEventHandler<IApplyConfigurationOperation, ApplyConfigurationActionRequiredEventArgs> ActionRequired = (s, e) => { };

    public event TypedEventHandler<IApplyConfigurationOperation, ConfigurationSetStateChangedEventArgs> ConfigurationSetStateChanged = (s, e) => { };

    public SDK.ApplyConfigurationResult? CompletionStatus { get; private set; }

    public SDK.ConfigurationSetChangeData ProgressData { get; private set; } =
        new SDK.ConfigurationSetChangeData(
            SDK.ConfigurationSetChangeEventType.Unknown,
            SDK.ConfigurationSetState.Unknown,
            SDK.ConfigurationUnitState.Unknown,
            new SDK.ConfigurationUnitResultInformation(null, null, null, SDK.ConfigurationUnitResultSource.None),
            new SDK.ConfigurationUnit(null, null, SDK.ConfigurationUnitState.Unknown, false, null, null, SDK.ConfigurationUnitIntent.Unknown));

    public void SetProgress(
        SDK.ConfigurationSetState state,
        HostGuestCommunication.ConfigurationSetChangeData? progressData,
        SDK.IExtensionAdaptiveCardSession2? adaptiveCardSession)
    {
        var sdkProgressData = GetSdkProgressData(state, progressData);

        if (sdkProgressData != null)
        {
            ProgressData = sdkProgressData;
            ConfigurationSetStateChanged?.Invoke(this, new(ProgressData));
        }

        if (adaptiveCardSession != null)
        {
            ActionRequired?.Invoke(this, new(adaptiveCardSession));
        }
    }

    public SDK.ApplyConfigurationResult CompleteOperation(HostGuestCommunication.ApplyConfigurationResult? completionStatus)
    {
        // If the completionStatus is not null, then the operation is completed.
        // if ((completionStatus != null) || (state == SDK.ConfigurationSetState.Completed))
        var sdkCompletionStatus = GetSdkConfigurationResult(completionStatus);
        if (sdkCompletionStatus == null)
        {
            // No apply configuration result was provided, but state is "Completed"
            // so create ApplyConfigurationResult with no error (meaning operation is completed).
            sdkCompletionStatus = new SDK.ApplyConfigurationResult(null, null);
        }

        return sdkCompletionStatus;
    }

    private SDK.ConfigurationSetChangeData GetSdkProgressData(
        SDK.ConfigurationSetState state,
        HostGuestCommunication.ConfigurationSetChangeData? progressData)
    {
        SDK.ConfigurationSetChangeData sdkProgressData;
        if (progressData != null)
        {
            SDK.ConfigurationUnitResultInformation resultInfo;
            if (progressData.ResultInformation != null)
            {
                resultInfo = new(
                    progressData.ResultInformation.ResultCode == HRESULT.S_OK ? null : new HResultException(progressData.ResultInformation.ResultCode, progressData.ResultInformation.Description),
                    progressData.ResultInformation.Description,
                    progressData.ResultInformation.Details,
                    (SDK.ConfigurationUnitResultSource)progressData.ResultInformation.ResultSource);
            }
            else
            {
                resultInfo = new(null, null, null, SDK.ConfigurationUnitResultSource.None);
            }

            SDK.ConfigurationUnit? sdkUnit = ConfigurationUnit(progressData.Unit);

            sdkProgressData = new SDK.ConfigurationSetChangeData(
                (SDK.ConfigurationSetChangeEventType)progressData.Change,
                (SDK.ConfigurationSetState)progressData.SetState,
                (SDK.ConfigurationUnitState)progressData.UnitState,
                resultInfo,
                sdkUnit);
        }
        else
        {
            sdkProgressData = new SDK.ConfigurationSetChangeData(
                SDK.ConfigurationSetChangeEventType.SetStateChanged,
                state,
                SDK.ConfigurationUnitState.Unknown,
                null,
                null);
        }

        return sdkProgressData;
    }

    private SDK.ApplyConfigurationResult? GetSdkConfigurationResult(HostGuestCommunication.ApplyConfigurationResult? completionStatus)
    {
        if (completionStatus != null)
        {
            SDK.OpenConfigurationSetResult? sdkOpenConfigurationSetResult = null;
            if (completionStatus.OpenConfigurationSetResult != null)
            {
                sdkOpenConfigurationSetResult = new(
                    completionStatus.OpenConfigurationSetResult.ResultCode == HRESULT.S_OK ?
                        null :
                        new HResultException(completionStatus.OpenConfigurationSetResult.ResultCode),
                    completionStatus.OpenConfigurationSetResult.Field,
                    completionStatus.OpenConfigurationSetResult.Value,
                    completionStatus.OpenConfigurationSetResult.Line,
                    completionStatus.OpenConfigurationSetResult.Column);
            }

            SDK.ApplyConfigurationSetResult? sdkApplyConfigurationSetResult = null;
            if (completionStatus.ApplyConfigurationSetResult != null)
            {
                var sdkUnitResults = new List<SDK.ApplyConfigurationUnitResult>();
                if (completionStatus.ApplyConfigurationSetResult.UnitResults != null)
                {
                    foreach (var unitResult in completionStatus.ApplyConfigurationSetResult.UnitResults)
                    {
                        SDK.ConfigurationUnit? sdkUnit = ConfigurationUnit(unitResult.Unit);

                        SDK.ConfigurationUnitResultInformation? sdkResultInfo = null;
                        if (unitResult.ResultInformation != null)
                        {
                            sdkResultInfo = new(
                                unitResult.ResultInformation.ResultCode == HRESULT.S_OK ?
                                    null :
                                    new HResultException(unitResult.ResultInformation.ResultCode, unitResult.ResultInformation.Description),
                                unitResult.ResultInformation.Description,
                                unitResult.ResultInformation.Details,
                                (SDK.ConfigurationUnitResultSource)unitResult.ResultInformation.ResultSource);
                        }

                        var configurationUnitState = sdkUnit != null ? sdkUnit.State : SDK.ConfigurationUnitState.Unknown;

                        sdkUnitResults.Add(new SDK.ApplyConfigurationUnitResult(
                            sdkUnit,
                            configurationUnitState,
                            unitResult.PreviouslyInDesiredState,
                            unitResult.RebootRequired,
                            sdkResultInfo));
                    }
                }

                sdkApplyConfigurationSetResult = new(
                    completionStatus.ApplyConfigurationSetResult.ResultCode == HRESULT.S_OK ?
                        null :
                        new HResultException(completionStatus.ApplyConfigurationSetResult.ResultCode),
                    sdkUnitResults.AsReadOnly());
            }

            var wasConfigurationSuccessful = completionStatus.ResultCode == HRESULT.S_OK;
            var isUnitResultsPresent = sdkApplyConfigurationSetResult?.UnitResults?.Count > 0;

            // If there was no error in the completionStatus or there are unit results we'll say our operation was successful.
            // Even if a unit result has errors, we will display this to the user.
            if (wasConfigurationSuccessful || isUnitResultsPresent)
            {
                return new SDK.ApplyConfigurationResult(sdkOpenConfigurationSetResult, sdkApplyConfigurationSetResult);
            }

            var hresultException = new HResultException(completionStatus.ResultCode, completionStatus.ResultDiagnosticText);

            return new SDK.ApplyConfigurationResult(hresultException, completionStatus.ResultDescription, hresultException.Message);
        }

        return null;
    }

    private SDK.ConfigurationUnit? ConfigurationUnit(HostGuestCommunication.ConfigurationUnit? configurationUnit)
    {
        if (configurationUnit != null)
        {
            List<SDK.ConfigurationUnit>? units = null;
            if (configurationUnit.Units != null)
            {
                units = new();
                foreach (var hostAndGuestUnit in configurationUnit.Units)
                {
                    units.Add(new(
                        hostAndGuestUnit.Type,
                        hostAndGuestUnit.Identifier,
                        (SDK.ConfigurationUnitState)hostAndGuestUnit.State,
                        hostAndGuestUnit.IsGroup,
                        null,
                        Dictionary2ValueSet(hostAndGuestUnit.Settings),
                        (SDK.ConfigurationUnitIntent)hostAndGuestUnit.Intent));
                }
            }

            return new(
                configurationUnit.Type,
                configurationUnit.Identifier,
                (SDK.ConfigurationUnitState)configurationUnit.State,
                configurationUnit.IsGroup,
                units,
                Dictionary2ValueSet(configurationUnit.Settings),
                (SDK.ConfigurationUnitIntent)configurationUnit.Intent);
        }

        return null;
    }

    private ValueSet? Dictionary2ValueSet(Dictionary<string, string>? dictionary)
    {
        if (dictionary == null)
        {
            return null;
        }

        var valueSet = new ValueSet();
        foreach (var kvp in dictionary)
        {
            valueSet.Add(kvp.Key, kvp.Value);
        }

        return valueSet;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
            }

            _disposed = true;
        }
    }

    public IAsyncOperation<SDK.ApplyConfigurationResult> StartAsync()
    {
        return Task.Run(() =>
        {
            return _virtualMachine.ApplyConfiguration(this);
        }).AsAsyncOperation();
    }
}
