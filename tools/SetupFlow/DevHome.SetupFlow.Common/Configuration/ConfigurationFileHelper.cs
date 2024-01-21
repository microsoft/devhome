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
using DevHome.Telemetry;
using Microsoft.Management.Configuration;
using Microsoft.Management.Configuration.Processor;
using Windows.Storage;
using Windows.Storage.Streams;

namespace DevHome.SetupFlow.Common.Configuration;

/// <summary>
/// Helper for applying a configuration file. This exists so that we can
/// use it in an elevated or non-elevated context.
/// </summary>
public class ConfigurationFileHelper
{
    public class ApplicationResult
    {
        public ApplyConfigurationSetResult Result
        {
            get;
        }

        public bool Succeeded => Result.ResultCode == null;

        public bool RequiresReboot => Result.UnitResults.Any(result => result.RebootRequired);

        public Exception ResultException => Result.ResultCode;

        public ApplicationResult(ApplyConfigurationSetResult result)
        {
            Result = result;
        }
    }

    private readonly Guid _activityId;
    private ConfigurationProcessor _processor;
    private ConfigurationSet _configSet;

    public ConfigurationFileHelper(Guid activityId)
    {
        _activityId = activityId;
    }

    /// <summary>
    /// Open configuration set from the provided <paramref name="content"/>.
    /// </summary>
    /// <param name="filePath">DSC configuration file path</param>
    /// <param name="content">DSC configuration file content</param>
    public async Task OpenConfigurationSetAsync(string filePath, string content)
    {
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(filePath);
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

            // Instead of reading the file content from the file path, use the
            // 'content' input parameter to ensure the data has not changed at
            // runtime on disk (important when running elevated)
            var inputStream = StringToStream(content);
            var openResult = _processor.OpenConfigurationSet(inputStream);
            _configSet = openResult.Set;
            if (_configSet == null)
            {
                throw new OpenConfigurationSetException(openResult.ResultCode, openResult.Field);
            }

            // Set input file path in the configuration set to inform the
            // processor about the working directory when applying the
            // configuration
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
    }

    public async Task<ApplicationResult> ApplyConfigurationAsync()
    {
        if (_processor == null || _configSet == null)
        {
            throw new InvalidOperationException();
        }

        Log.Logger?.ReportInfo(Log.Component.Configuration, "Starting to apply configuration set");
        var result = await _processor.ApplySetAsync(_configSet, ApplyConfigurationSetFlags.None);

        foreach (var unitResult in result.UnitResults)
        {
            TelemetryFactory.Get<ITelemetry>().Log("ConfigurationFile_UnitResult", LogLevel.Critical, new ConfigurationUnitResultEvent(unitResult), _activityId);
        }

        TelemetryFactory.Get<ITelemetry>().Log("ConfigurationFile_Result", LogLevel.Critical, new ConfigurationSetResultEvent(_configSet, result), _activityId);

        Log.Logger?.ReportInfo(Log.Component.Configuration, $"Apply configuration finished. HResult: {result.ResultCode?.HResult}");
        return new ApplicationResult(result);
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

    /// <summary>
    /// Convert a string to an input stream
    /// </summary>
    /// <param name="str">Target string</param>
    /// <returns>Input stream</returns>
    private InMemoryRandomAccessStream StringToStream(string str)
    {
        InMemoryRandomAccessStream result = new();
        using (DataWriter writer = new(result))
        {
            writer.UnicodeEncoding = UnicodeEncoding.Utf8;
            writer.WriteString(str);
            writer.StoreAsync().AsTask().Wait();
            writer.DetachStream();
        }

        result.Seek(0);
        return result;
    }
}
