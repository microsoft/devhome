// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DevHome.Services.DesiredStateConfiguration.Contracts;
using DevHome.Services.DesiredStateConfiguration.Exceptions;
using DevHome.Services.DesiredStateConfiguration.Models;
using DevHome.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Management.Configuration;
using Windows.Storage.Streams;

namespace DevHome.Services.DesiredStateConfiguration.Services;

internal sealed class DSCOperations : IDSCOperations
{
    private readonly ILogger _logger;
    private const string PowerShellHandlerIdentifier = "pwsh";

    public DSCOperations(ILogger<DSCOperations> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IDSCApplicationResult> ApplyConfigurationAsync(IDSCFile file, Guid activityId)
    {
        var processor = await CreateConfigurationProcessorAsync();
        var configSet = await OpenConfigurationSetAsync(file, processor);

        _logger.LogInformation("Starting to apply configuration set");
        var outOfProcResult = await processor.ApplySetAsync(configSet, ApplyConfigurationSetFlags.None);
        var inProcResult = new DSCApplicationResult(configSet, outOfProcResult);
        _logger.LogInformation($"Apply configuration finished. HResult: {inProcResult.ResultException?.HResult}");
        ReportApplyConfigurationResult(inProcResult, activityId);
        return inProcResult;
    }

    /// <inheritdoc />
    public async Task<IDSCSet> GetConfigurationUnitDetailsAsync(IDSCFile file)
    {
        var processor = await CreateConfigurationProcessorAsync();
        var configSet = await OpenConfigurationSetAsync(file, processor);

        _logger.LogInformation("Getting configuration unit details");
        await processor.GetSetDetailsAsync(configSet, ConfigurationUnitDetailFlags.ReadOnly);
        return new DSCSet(configSet);
    }

    /// <inheritdoc />
    public async Task ValidateConfigurationAsync(IDSCFile file)
    {
        // Try to open the configuration file to validate it.
        _logger.LogInformation("Validating configuration file");
        var processor = await CreateConfigurationProcessorAsync();
        await OpenConfigurationSetAsync(file, processor);
    }

    /// <summary>
    /// Report the result of applying a configuration set
    /// </summary>
    /// <param name="result">Result of applying the configuration set</param>
    /// <param name="activityId">Activity ID</param>
    private void ReportApplyConfigurationResult(IDSCApplicationResult result, Guid activityId)
    {
        foreach (var unitResult in result.UnitResults)
        {
            TelemetryFactory.Get<ITelemetry>().Log("ConfigurationFile_UnitResult", Telemetry.LogLevel.Critical, new ConfigurationUnitResultEvent(unitResult), activityId);
        }

        TelemetryFactory.Get<ITelemetry>().Log("ConfigurationFile_Result", Telemetry.LogLevel.Critical, new ConfigurationSetResultEvent(result), activityId);
    }

    /// <summary>
    /// Create a configuration processor using DSC configuration API
    /// </summary>
    /// <returns>Configuration processor</returns>
    private async Task<ConfigurationProcessor> CreateConfigurationProcessorAsync()
    {
        ConfigurationStaticFunctions config = new();
        var factory = await config.CreateConfigurationSetProcessorFactoryAsync(PowerShellHandlerIdentifier);

        // Create and configure the configuration processor.
        var processor = config.CreateConfigurationProcessor(factory);
        processor.Caller = nameof(DevHome);
        processor.Diagnostics += LogConfigurationDiagnostics;
        processor.MinimumLevel = DiagnosticLevel.Verbose;
        return processor;
    }

    /// <summary>
    /// Open a configuration set using DSC configuration API
    /// </summary>
    /// <param name="file">Configuration file</param>
    /// <returns>Configuration set</returns>
    /// <exception cref="OpenConfigurationSetException">Thrown when the configuration set cannot be opened</exception>
    private async Task<ConfigurationSet> OpenConfigurationSetAsync(IDSCFile file, ConfigurationProcessor processor)
    {
        var inputStream = await StringToStreamAsync(file.Content);
        var openConfigResult = processor.OpenConfigurationSet(inputStream);
        var configSet = openConfigResult.Set ?? throw new OpenConfigurationSetException(openConfigResult.ResultCode, openConfigResult.Field, openConfigResult.Value);

        // Set input file path in the configuration set to inform the
        // processor about the working directory when applying the
        // configuration
        configSet.Name = file.Name;
        configSet.Origin = file.DirectoryPath;
        configSet.Path = file.Path;
        return configSet;
    }

    /// <summary>
    /// Map configuration diagnostics to logger
    /// </summary>
    /// <param name="sender">The event sender</param>
    /// <param name="diagnosticInformation">Diagnostic information</param>
    private void LogConfigurationDiagnostics(object sender, IDiagnosticInformation diagnosticInformation)
    {
        switch (diagnosticInformation.Level)
        {
            case DiagnosticLevel.Warning:
                _logger.LogWarning(diagnosticInformation.Message);
                return;
            case DiagnosticLevel.Error:
                _logger.LogError(diagnosticInformation.Message);
                return;
            case DiagnosticLevel.Critical:
                _logger.LogCritical(diagnosticInformation.Message);
                return;
            case DiagnosticLevel.Verbose:
                _logger.LogTrace(diagnosticInformation.Message);
                return;
            case DiagnosticLevel.Informational:
            default:
                _logger.LogInformation(diagnosticInformation.Message);
                return;
        }
    }

    /// <summary>
    /// Convert a string to an input stream
    /// </summary>
    /// <param name="str">Target string</param>
    /// <returns>Input stream</returns>
    private static async Task<InMemoryRandomAccessStream> StringToStreamAsync(string str)
    {
        InMemoryRandomAccessStream result = new();
        using (DataWriter writer = new(result))
        {
            writer.UnicodeEncoding = UnicodeEncoding.Utf8;
            writer.WriteString(str);
            await writer.StoreAsync();
            writer.DetachStream();
        }

        result.Seek(0);
        return result;
    }
}
