// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DevHome.Logging;
using DevHome.SetupFlow.Common.Exceptions;
using Microsoft.Management.Configuration;
using Microsoft.Management.Configuration.Processor;
using Windows.Storage;

namespace DevHome.SetupFlow.Common.Configuration;

/// <summary>
/// Helper for applying a configuration file. This exists so that we can
/// use it in an elevated or non-elevated context.
/// </summary>
public class ConfigurationFileHelper
{
    private readonly StorageFile _file;
    private readonly Logger _logger;
    private ConfigurationProcessor _processor;
    private ConfigurationSet _configSet;
    private ApplyConfigurationSetResult _result;

    public ConfigurationFileHelper(StorageFile file)
    {
        _file = file;
        _logger = new ComponentLogger("ConfigurationFile", "SetupFlow").Logger;
    }

    public async Task OpenConfigurationSetAsync()
    {
        try
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidDataException();
            var rootDirectory = Path.GetDirectoryName(assemblyDirectory) ?? throw new InvalidDataException();
            var modulesPath = Path.Combine(rootDirectory, "ExternalModules");

            var properties = new ConfigurationProcessorFactoryProperties();
            properties.AdditionalModulePaths = new List<string>() { modulesPath };
            var factory = new ConfigurationSetProcessorFactory(ConfigurationProcessorType.Hosted, properties);
            factory.Diagnostics += (sender, args) => LogConfigurationDiagnostics(nameof(ConfigurationSetProcessorFactory), args);

            _processor = new ConfigurationProcessor(factory);
            _processor.Diagnostics += (sender, args) => LogConfigurationDiagnostics(nameof(ConfigurationSetProcessorFactory), args);

            _logger.ReportInfo($"Opening configuration set from path {_file.Path}");
            var openResult = _processor.OpenConfigurationSet(await _file.OpenReadAsync());
            _configSet = openResult.Set;
            if (_configSet == null)
            {
                throw new OpenConfigurationSetException(openResult.ResultCode, openResult.Field);
            }
        }
        catch
        {
            _processor = null;
            _configSet = null;
            throw;
        }
    }

    public async Task ApplyConfigurationAsync()
    {
        if (_processor == null || _configSet == null)
        {
            throw new InvalidOperationException();
        }

        _logger.ReportInfo("Starting to apply configuration set");
        _result = await _processor.ApplySetAsync(_configSet, ApplyConfigurationSetFlags.None);
        _logger.ReportInfo($"Apply configuration finished. HResult: {_result.ResultCode?.HResult}");
    }

    public bool ApplicationSucceeded => _result.ResultCode == null;

    public bool ResultRequiresReboot => _result.UnitResults.Any(result => result.RebootRequired);

    public Exception ResultException => _result.ResultCode;

    private void LogConfigurationDiagnostics(string sourceComponent, DiagnosticInformation diagnosticInformation)
    {
        switch (diagnosticInformation.Level)
        {
            case DiagnosticLevel.Verbose:
                _logger.ReportDebug(sourceComponent, diagnosticInformation.Message);
                return;
            case DiagnosticLevel.Informational:
                _logger.ReportInfo(sourceComponent, diagnosticInformation.Message);
                return;
            case DiagnosticLevel.Warning:
                _logger.ReportWarn(sourceComponent, diagnosticInformation.Message);
                return;
            case DiagnosticLevel.Error:
                _logger.ReportError(sourceComponent, diagnosticInformation.Message);
                return;
            case DiagnosticLevel.Critical:
                _logger.ReportCritical(sourceComponent, diagnosticInformation.Message);
                return;
        }
    }
}
