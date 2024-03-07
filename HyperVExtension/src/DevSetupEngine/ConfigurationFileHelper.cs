// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Buffers;
using Microsoft.Management.Configuration;
using Microsoft.Management.Configuration.Processor;
using Microsoft.Management.Configuration.SetProcessorFactory;
using Windows.Storage.Streams;
using WinRT;
using DevSetupEngineTypes = Microsoft.Windows.DevHome.DevSetupEngine;
using WinGet = Microsoft.Management.Configuration;

namespace HyperVExtension.DevSetupEngine;

/// <summary>
/// Helper for applying a configuration file. This exists so that we can
/// use it in an elevated or non-elevated context.
/// </summary>
public class ConfigurationFileHelper
{
    public class ApplicationResult
    {
        public WinGet.ApplyConfigurationSetResult Result
        {
            get;
        }

        public bool Succeeded => Result.ResultCode == null;

        public bool RequiresReboot => Result.UnitResults.Any(result => result.RebootRequired);

        public Exception ResultException => Result.ResultCode;

        public ApplicationResult(WinGet.ApplyConfigurationSetResult result)
        {
            Result = result;
        }
    }

    private WinGet.ConfigurationProcessor? _processor;
    private WinGet.ConfigurationSet? _configSet;

    public ConfigurationFileHelper()
    {
    }

    /// <summary>
    /// Open configuration set from the provided <paramref name="content"/>.
    /// </summary>
    /// <param name="content">DSC configuration file content</param>
    private ConfigurationResultTypes.OpenConfigurationSetResult OpenConfigurationSet(string content)
    {
        try
        {
            var modulesPath = Path.Combine(AppContext.BaseDirectory, @"runtimes\win\lib\net8.0\Modules");
            var externalModulesPath = Path.Combine(AppContext.BaseDirectory, "ExternalModules");

            PowerShellConfigurationSetProcessorFactory pwshFactory = new PowerShellConfigurationSetProcessorFactory();
            pwshFactory.Policy = PowerShellConfigurationProcessorPolicy.Unrestricted;
            pwshFactory.AdditionalModulePaths = new List<string>() { modulesPath, externalModulesPath };
            Logging.Logger()?.ReportInfo($"Additional module paths: {string.Join(", ", pwshFactory.AdditionalModulePaths)}");

            _processor = new ConfigurationProcessor(pwshFactory);
            _processor.MinimumLevel = WinGet.DiagnosticLevel.Verbose;
            _processor.Diagnostics += (sender, args) => LogConfigurationDiagnostics(args);
            _processor.Caller = nameof(DevSetupEngine);

            var inputStream = StringToStream(content);
            var openResult = _processor.OpenConfigurationSet(inputStream);
            _configSet = openResult.Set;
            if (_configSet == null)
            {
                throw new OpenConfigurationSetException(openResult);
            }

            return new ConfigurationResultTypes.OpenConfigurationSetResult(openResult.ResultCode, openResult.Field, openResult.Value, openResult.Line, openResult.Column);
        }
        catch (OpenConfigurationSetException ex)
        {
            ConfigurationResultTypes.OpenConfigurationSetResult result =
                new(ex.OpenConfigurationSetResult.ResultCode, ex.OpenConfigurationSetResult.Field, ex.OpenConfigurationSetResult.Value, ex.OpenConfigurationSetResult.Line, ex.OpenConfigurationSetResult.Column);

            _processor = null;
            _configSet = null;
            return result;
        }
        catch (Exception ex)
        {
            ConfigurationResultTypes.OpenConfigurationSetResult result =
                new(ex, string.Empty, string.Empty, 0, 0);

            _processor = null;
            _configSet = null;
            return result;
        }
    }

    private async Task<DevSetupEngineTypes.IApplyConfigurationSetResult> ApplyConfigurationSetAsync(IProgress<DevSetupEngineTypes.IConfigurationSetChangeData> progress)
    {
        if (_processor == null || _configSet == null)
        {
            throw new InvalidOperationException();
        }

        Logging.Logger()?.ReportInfo("Starting to apply configuration set");
        var applySetOperation = _processor.ApplySetAsync(_configSet, WinGet.ApplyConfigurationSetFlags.None);
        var progressWatcher = new ApplyConfigurationProgressWatcher(progress);
        applySetOperation.Progress += progressWatcher.Watcher;
        var result = await applySetOperation;

        Logging.Logger()?.ReportInfo($"Apply configuration finished. HResult: {result.ResultCode?.HResult}");

        var unitResults = new List<DevSetupEngineTypes.IApplyConfigurationUnitResult>();
        foreach (var unitResult in result.UnitResults)
        {
            var unit = new ConfigurationResultTypes.ConfigurationUnit(
                unitResult.Unit.Type,
                unitResult.Unit.Identifier,
                (DevSetupEngineTypes.ConfigurationUnitState)unitResult.Unit.State,
                false,
                null,
                unitResult.Unit.Settings,
                (DevSetupEngineTypes.ConfigurationUnitIntent)unitResult.Unit.Intent);

            var resultInfo = new ConfigurationResultTypes.ConfigurationUnitResultInformation(
                unitResult.ResultInformation.ResultCode,
                unitResult.ResultInformation.Description,
                unitResult.ResultInformation.Details,
                (DevSetupEngineTypes.ConfigurationUnitResultSource)unitResult.ResultInformation.ResultSource);

            var configurationUnitResult = new ConfigurationResultTypes.ApplyConfigurationUnitResult(
                unit,
                (DevSetupEngineTypes.ConfigurationUnitState)unitResult.State,
                unitResult.PreviouslyInDesiredState,
                unitResult.RebootRequired,
                resultInfo);

            unitResults.Add(configurationUnitResult);
        }

        var applyConfigurationSetResult = new ConfigurationResultTypes.ApplyConfigurationSetResult(result.ResultCode, unitResults);

        return applyConfigurationSetResult;
    }

    public async Task<DevSetupEngineTypes.IApplyConfigurationResult> ApplyConfigurationAsync(string content, IProgress<DevSetupEngineTypes.IConfigurationSetChangeData> progress)
    {
        var openConfigurationSetResult = OpenConfigurationSet(content);
        if (openConfigurationSetResult.ResultCode != null)
        {
            return new ConfigurationResultTypes.ApplyConfigurationResult(openConfigurationSetResult.ResultCode, string.Empty, openConfigurationSetResult, null);
        }

        var applyConfigurationSetResult = await ApplyConfigurationSetAsync(progress);

        return new ConfigurationResultTypes.ApplyConfigurationResult(applyConfigurationSetResult.ResultCode, string.Empty, openConfigurationSetResult, applyConfigurationSetResult);
    }

    private void LogConfigurationDiagnostics(WinGet.IDiagnosticInformation diagnosticInformation)
    {
        Logging.Logger()?.ReportInfo($"WinGet: {diagnosticInformation.Message}");

        var sourceComponent = nameof(WinGet.ConfigurationProcessor);
        switch (diagnosticInformation.Level)
        {
            case WinGet.DiagnosticLevel.Warning:
                Logging.Logger()?.ReportWarn(sourceComponent, diagnosticInformation.Message);
                return;
            case WinGet.DiagnosticLevel.Error:
                Logging.Logger()?.ReportError(sourceComponent, diagnosticInformation.Message);
                return;
            case WinGet.DiagnosticLevel.Critical:
                Logging.Logger()?.ReportCritical(sourceComponent, diagnosticInformation.Message);
                return;
            case WinGet.DiagnosticLevel.Verbose:
            case WinGet.DiagnosticLevel.Informational:
            default:
                Logging.Logger()?.ReportInfo(sourceComponent, diagnosticInformation.Message);
                return;
        }
    }

    /// <summary>
    /// Convert a string to an input stream
    /// </summary>
    /// <param name="str">Target string</param>
    /// <returns>Input stream</returns>
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
    private IInputStream StringToStream(string str)
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
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
