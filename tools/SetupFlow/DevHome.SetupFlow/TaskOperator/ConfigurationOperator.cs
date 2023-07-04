// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Exceptions;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.TelemetryEvents;
using DevHome.SetupFlow.Contract.TaskOperator;
using DevHome.Telemetry;
using Microsoft.Management.Configuration;
using Microsoft.Management.Configuration.Processor;
using Windows.Foundation;
using Windows.Storage;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.TaskOperator;
public class ConfigurationOperator : IConfigurationOperator
{
    private ConfigurationProcessor _processor;
    private ConfigurationSet _configSet;

    public IAsyncAction OpenConfigurationSetAsync(StorageFile file)
    {
        return Task.Run(async () =>
        {
            Log.Logger?.ReportInfo(Log.Component.Configuration, $"Opening configuration set from file: {file.Path}");

            try
            {
                var modulesPath = Path.Combine(AppContext.BaseDirectory, @"runtimes\win\lib\net6.0\Modules");
                var externalModulesPath = Path.Combine(AppContext.BaseDirectory, "ExternalModules");
                var properties = new ConfigurationProcessorFactoryProperties();
                properties.Policy = ConfigurationProcessorPolicy.Unrestricted;
                properties.AdditionalModulePaths = new List<string>() { modulesPath, externalModulesPath };
                Log.Logger?.ReportInfo(Log.Component.Configuration, $"Additional module paths: {string.Join(", ", properties.AdditionalModulePaths)}");
                var factory = new ConfigurationSetProcessorFactory(ConfigurationProcessorType.Hosted, properties);

                _processor = new ConfigurationProcessor(factory);
                _processor.MinimumLevel = DiagnosticLevel.Verbose;
                _processor.Diagnostics += (sender, args) => LogConfigurationDiagnostics(args);
                _processor.Caller = nameof(DevHome);

                Log.Logger?.ReportInfo(Log.Component.Configuration, $"Opening configuration set from path {file.Path}");
                var parentDir = await file.GetParentAsync();
                var openResult = _processor.OpenConfigurationSet(await file.OpenReadAsync());
                _configSet = openResult.Set;
                if (_configSet == null)
                {
                    throw new OpenConfigurationSetException(openResult.ResultCode, openResult.Field);
                }

                // Set input file path to the configuration set
                _configSet.Name = file.Name;
                _configSet.Origin = parentDir.Path;
                _configSet.Path = file.Path;
            }
            catch
            {
                _processor = null;
                _configSet = null;
                throw;
            }
        }).AsAsyncAction();
    }

    public IAsyncOperation<IApplyConfigurationResult> ApplyConfigurationAsync()
    {
        return Task.Run<IApplyConfigurationResult>(async () =>
        {
            if (_processor == null || _configSet == null)
            {
                throw new InvalidOperationException("Cannot apply configuration without opening it first");
            }

            try
            {
                Log.Logger?.ReportInfo(Log.Component.Configuration, "Starting configuration set application");
                var result = await _processor.ApplySetAsync(_configSet, ApplyConfigurationSetFlags.None);

                foreach (var unitResult in result.UnitResults)
                {
                    TelemetryFactory.Get<ITelemetry>().Log("ConfigurationFile_UnitResult", LogLevel.Critical, new ConfigurationUnitResultEvent(unitResult));
                }

                TelemetryFactory.Get<ITelemetry>().Log("ConfigurationFile_Result", LogLevel.Critical, new ConfigurationSetResultEvent(_configSet, result));

                Log.Logger?.ReportInfo(Log.Component.Configuration, $"Apply configuration finished. HResult: {result.ResultCode?.HResult}");

                return new ApplyConfigurationResult
                {
                    Attempted = true,
                    Succeeded = result.ResultCode == null,
                    RebootRequired = result.UnitResults.Any(result => result.RebootRequired),
                    UnitResults = result.UnitResults.Select(unitResult => new ConfigurationUnitResult
                    {
                        UnitName = unitResult.Unit.UnitName,
                        Intent = unitResult.Unit.Intent.ToString(),
                        IsSkipped = unitResult.State == ConfigurationUnitState.Skipped,
                        HResult = unitResult.ResultInformation?.ResultCode?.HResult ?? HRESULT.S_OK,
                    }).ToList<IConfigurationUnitResult>(),
                };
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.Configuration, $"Failed to apply configuration.", e);
                return new ApplyConfigurationResult
                {
                    Attempted = true,
                    Succeeded = false,
                };
            }
        }).AsAsyncOperation();
    }

    private void LogConfigurationDiagnostics(DiagnosticInformation diagnosticInformation)
    {
        var sourceComponent = nameof(ConfigurationProcessor);
        switch (diagnosticInformation.Level)
        {
            case DiagnosticLevel.Warning:
                Log.Logger?.ReportWarn(Log.Component.Configuration, sourceComponent, diagnosticInformation.Message);
                return;
            case DiagnosticLevel.Error:
                Log.Logger?.ReportError(Log.Component.Configuration, sourceComponent, diagnosticInformation.Message);
                return;
            case DiagnosticLevel.Critical:
                Log.Logger?.ReportCritical(Log.Component.Configuration, sourceComponent, diagnosticInformation.Message);
                return;
            case DiagnosticLevel.Verbose:
            case DiagnosticLevel.Informational:
            default:
                Log.Logger?.ReportInfo(Log.Component.Configuration, sourceComponent, diagnosticInformation.Message);
                return;
        }
    }
}
