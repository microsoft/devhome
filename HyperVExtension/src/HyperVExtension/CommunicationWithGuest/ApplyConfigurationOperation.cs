// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using HyperVExtension.HostGuestCommunication;
using HyperVExtension.Providers;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;
using Windows.Win32.Foundation;

using SDK = Microsoft.Windows.DevHome.SDK;

namespace HyperVExtension.CommunicationWithGuest;

internal sealed class ApplyConfigurationOperation : IApplyConfigurationOperation, IDisposable
{
    private readonly IComputeSystem _computeSystem;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _disposed;

    public ApplyConfigurationOperation(IComputeSystem computeSystem)
    {
        _computeSystem = computeSystem;
    }

    public ApplyConfigurationOperation(IComputeSystem computeSystem, Exception result, string? resultDescription = null)
    {
        _computeSystem = computeSystem;
        CompletionStatus = new SDK.ApplyConfigurationResult(
            result,
            resultDescription,
            null,
            null);
    }

    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    public event TypedEventHandler<IComputeSystem, SDK.ApplyConfigurationResult>? Completed;

    public event TypedEventHandler<IComputeSystem, SDK.ConfigurationSetChangeData>? Progress;

    public SDK.ApplyConfigurationResult? CompletionStatus { get; private set; }

    public SDK.ConfigurationSetChangeData ProgressData { get; private set; } =
        new SDK.ConfigurationSetChangeData(
            SDK.ConfigurationSetChangeEventType.Unknown,
            SDK.ConfigurationSetState.Unknown,
            SDK.ConfigurationUnitState.Unknown,
            new SDK.ConfigurationUnitResultInformation(null, null, null, SDK.ConfigurationUnitResultSource.None),
            new SDK.ConfigurationUnit(null, null, SDK.ConfigurationUnitState.Unknown, false, null),
            null);

    public void Cancel() => throw new NotImplementedException();

    public void SetState(
        SDK.ConfigurationSetState state,
        HostGuestCommunication.ConfigurationSetChangeData? progressData,
        HostGuestCommunication.ApplyConfigurationResult? completionStatus,
        SDK.IExtensionAdaptiveCardSession2? adaptiveCardSession)
    {
        var sdkProgressData = GetSdkProgressData(state, progressData, adaptiveCardSession);

        if (sdkProgressData != null)
        {
            ProgressData = sdkProgressData;
            Progress?.Invoke(_computeSystem, ProgressData);
        }

        // If the completionStatus is not null, then the operation is completed.
        if ((completionStatus != null) || (state == SDK.ConfigurationSetState.Completed))
        {
            var sdkCompletionStatus = GetSdkConfigurationResult(completionStatus);
            if (sdkCompletionStatus == null)
            {
                // No apply configuration result was provided, but state is "Completed"
                // so create ApplyConfigurationResult with no error (meaning operation is completed).
                sdkCompletionStatus = new SDK.ApplyConfigurationResult(null, null, null, null);
            }

            CompletionStatus = sdkCompletionStatus;
            Completed?.Invoke(_computeSystem, CompletionStatus);
        }
    }

    private SDK.ConfigurationSetChangeData GetSdkProgressData(
        SDK.ConfigurationSetState state,
        HostGuestCommunication.ConfigurationSetChangeData? progressData,
        SDK.IExtensionAdaptiveCardSession2? adaptiveCardSession)
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
                sdkUnit,
                adaptiveCardSession);
        }
        else
        {
            sdkProgressData = new SDK.ConfigurationSetChangeData(
                SDK.ConfigurationSetChangeEventType.SetStateChanged,
                state,
                SDK.ConfigurationUnitState.Unknown,
                null,
                null,
                adaptiveCardSession);
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

                        sdkUnitResults.Add(new SDK.ApplyConfigurationUnitResult(
                            sdkUnit,
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

            return new SDK.ApplyConfigurationResult(
                completionStatus.ResultCode == HRESULT.S_OK ?
                    null :
                    new HResultException(completionStatus.ResultCode),
                completionStatus.ResultDescription,
                sdkOpenConfigurationSetResult,
                sdkApplyConfigurationSetResult);
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
                foreach (var u in configurationUnit.Units)
                {
                    units.Add(new(
                        u.Type,
                        u.Identifier,
                        (SDK.ConfigurationUnitState)u.State,
                        u.IsGroup,
                        null));
                }
            }

            return new(
                configurationUnit.Type,
                configurationUnit.Identifier,
                (SDK.ConfigurationUnitState)configurationUnit.State,
                configurationUnit.IsGroup,
                units);
        }

        return null;
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
}
