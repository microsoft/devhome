// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using DevHome.Services.DesiredStateConfiguration.Exceptions;
using DevHome.Services.DesiredStateConfiguration.TelemetryEvents;
using DevHome.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Management.Configuration;
using Windows.Storage.Streams;

namespace DevHome.Services.DesiredStateConfiguration.Models;

/// <summary>
/// Model class for a YAML configuration file
/// </summary>
public class DSCConfiguration
{
    private const string PowerShellHandlerIdentifier = "pwsh";
    private readonly ILogger _logger;
    private readonly FileInfo _fileInfo;

    public DSCConfiguration(ILogger logger, string filePath)
    {
        _logger = logger;
        _fileInfo = new FileInfo(filePath);
        Content = LoadContent(logger, _fileInfo);
    }

    public DSCConfiguration(ILogger logger, string filePath, string content)
    {
        _logger = logger;
        _fileInfo = new FileInfo(filePath);
        Content = content;
    }

    /// <summary>
    /// Gets the configuration file name
    /// </summary>
    public string Name => _fileInfo.Name;

    /// <summary>
    /// Gets the configuration file path
    /// </summary>
    public string Path => _fileInfo.FullName;

    /// <summary>
    /// Gets the configuration file directory path
    /// </summary>
    public string DirectoryPath => _fileInfo.Directory.FullName;

    /// <summary>
    /// Gets the configuration file content
    /// </summary>
    public string Content { get; }

    public async Task<DSCApplicationResult> ApplyConfigurationAsync(ConfigurationProcessor processor, ConfigurationSet configSet, Guid activityId)
    {
        _logger.LogInformation("Starting to apply configuration set");
        var result = await processor.ApplySetAsync(configSet, ApplyConfigurationSetFlags.None);

        foreach (var unitResult in result.UnitResults)
        {
            TelemetryFactory.Get<ITelemetry>().Log("ConfigurationFile_UnitResult", Telemetry.LogLevel.Critical, new ConfigurationUnitResultEvent(unitResult), activityId);
        }

        TelemetryFactory.Get<ITelemetry>().Log("ConfigurationFile_Result", Telemetry.LogLevel.Critical, new ConfigurationSetResultEvent(configSet, result), activityId);
        _logger.LogInformation($"Apply configuration finished. HResult: {result.ResultCode?.HResult}");
        return new(result);
    }

    public async Task ResolveConfigurationUnitDetails(ConfigurationProcessor processor, ConfigurationSet configSet)
    {
        await processor.GetSetDetailsAsync(configSet, ConfigurationUnitDetailFlags.ReadOnly);
    }

    /// <summary>
    /// Load configuration file content
    /// </summary>
    /// <returns>Configuration file content</returns>
    private static string LoadContent(ILogger logger, FileInfo fileInfo)
    {
        logger.LogInformation($"Loading configuration file content from {fileInfo.FullName}");
        using var text = fileInfo.OpenText();
        return text.ReadToEnd();
    }

    /// <summary>
    /// Open a configuration set using DSC configuration API
    /// </summary>
    /// <returns>Configuration set</returns>
    /// <exception cref="OpenConfigurationSetException">Thrown when the configuration set cannot be opened</exception>
    private async Task<ConfigurationSet> OpenConfigurationSetAsync()
    {
        var processor = await CreateConfigurationProcessorAsync();
        var inputStream = StringToStream(Content);

        var openConfigResult = processor.OpenConfigurationSet(inputStream);
        var configSet = openConfigResult.Set ?? throw new OpenConfigurationSetException(openConfigResult.ResultCode, openConfigResult.Field, openConfigResult.Value);

        // Set input file path in the configuration set to inform the
        // processor about the working directory when applying the
        // configuration
        configSet.Name = Name;
        configSet.Origin = DirectoryPath;
        configSet.Path = Path;
        return configSet;
    }

    private async Task<ConfigurationProcessor> CreateConfigurationProcessorAsync()
    {
        ConfigurationStaticFunctions config = new();
        var factory = await config.CreateConfigurationSetProcessorFactoryAsync(PowerShellHandlerIdentifier).AsTask();

        // Create and configure the configuration processor.
        var processor = config.CreateConfigurationProcessor(factory);
        processor.Caller = nameof(DevHome);
        processor.Diagnostics += LogConfigurationDiagnostics;
        processor.MinimumLevel = DiagnosticLevel.Verbose;
        return processor;
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
            case DiagnosticLevel.Critical:
                _logger.LogError(diagnosticInformation.Message);
                _logger.LogError(diagnosticInformation.Message);
                return;
            case DiagnosticLevel.Verbose:
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
