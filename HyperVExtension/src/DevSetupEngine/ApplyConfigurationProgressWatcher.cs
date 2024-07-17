// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.DevSetupEngine.ConfigurationResultTypes;
using Microsoft.CodeAnalysis.Emit;
using Windows.Foundation;

using DevSetupEngineTypes = Microsoft.Windows.DevHome.DevSetupEngine;
using WinGet = Microsoft.Management.Configuration;

namespace HyperVExtension.DevSetupEngine;

/// <summary>
/// Class to handle WinGet Progress event.
/// Converts WinGet progress data (IConfigurationSetChangeData) into our own version of IConfigurationSetChangeData
/// to pass back to the caller.
/// </summary>
internal sealed class ApplyConfigurationProgressWatcher
{
    private readonly IProgress<DevSetupEngineTypes.IConfigurationSetChangeData> _progress;
    private bool _isFirstProgress = true;

    public ApplyConfigurationProgressWatcher(IProgress<DevSetupEngineTypes.IConfigurationSetChangeData> progress)
    {
        _progress = progress;
    }

    internal void Watcher(IAsyncOperationWithProgress<WinGet.ApplyConfigurationSetResult, WinGet.ConfigurationSetChangeData> operation, WinGet.ConfigurationSetChangeData data)
    {
        if (_isFirstProgress)
        {
            _isFirstProgress = false;

            // If our first progress callback contains partial results, output them as if they had been called through progress
            WinGet.ApplyConfigurationSetResult partialResult = operation.GetResults();

            foreach (var unitResult in partialResult.UnitResults)
            {
                HandleUnitProgress(unitResult.Unit, unitResult.State, unitResult.ResultInformation);
            }
        }

        switch (data.Change)
        {
            case WinGet.ConfigurationSetChangeEventType.SetStateChanged:
                Logging.Logger()?.ReportInfo($"  - Set State: {data.SetState}");
                break;
            case WinGet.ConfigurationSetChangeEventType.UnitStateChanged:
                HandleUnitProgress(data.Unit, data.UnitState, data.ResultInformation);
                break;
        }
    }

    private void HandleUnitProgress(WinGet.ConfigurationUnit unit, WinGet.ConfigurationUnitState unitState, WinGet.IConfigurationUnitResultInformation resultInformation)
    {
        switch (unitState)
        {
            case WinGet.ConfigurationUnitState.Pending:
                break;
            case WinGet.ConfigurationUnitState.InProgress:
            case WinGet.ConfigurationUnitState.Completed:
            case WinGet.ConfigurationUnitState.Skipped:
                Logging.Logger()?.ReportInfo($"  - Unit: {unit.Identifier} [{unit.InstanceIdentifier}]");
                Logging.Logger()?.ReportInfo($"    Unit State: {unitState}");
                if (resultInformation.ResultCode != null)
                {
                    Logging.Logger()?.ReportInfo($"    HRESULT: [0x{resultInformation.ResultCode.HResult:X8}]");
                    Logging.Logger()?.ReportInfo($"    Reason: {resultInformation.Description}");
                }

                break;
            case WinGet.ConfigurationUnitState.Unknown:
                break;
        }

        if (_progress != null)
        {
            var resultInfo = new ConfigurationResultTypes.ConfigurationUnitResultInformation(
                    resultInformation.ResultCode,
                    resultInformation.Description,
                    resultInformation.Details,
                    (DevSetupEngineTypes.ConfigurationUnitResultSource)resultInformation.ResultSource);

            var configurationUnit = new ConfigurationResultTypes.ConfigurationUnit(
                unit.Type,
                unit.Identifier,
                (DevSetupEngineTypes.ConfigurationUnitState)unit.State,
                false,
                null,
                unit.Settings,
                (DevSetupEngineTypes.ConfigurationUnitIntent)unit.Intent);

            var configurationSetChangeData = new ConfigurationSetChangeData(
                DevSetupEngineTypes.ConfigurationSetChangeEventType.UnitStateChanged,
                DevSetupEngineTypes.ConfigurationSetState.Unknown,
                (DevSetupEngineTypes.ConfigurationUnitState)unitState,
                resultInfo,
                configurationUnit);

            _progress.Report(configurationSetChangeData);
        }
    }
}
