// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Management.Configuration;
using Serilog;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Store.Preview.InstallControl;
using Windows.Foundation;
using Windows.Storage.Streams;
using DevSetupEngineTypes = Microsoft.Windows.DevHome.DevSetupEngine;
using WinGet = Microsoft.Management.Configuration;

namespace HyperVExtension.DevSetupEngine;

/// <summary>
/// Helper for applying a configuration file. This exists so that we can
/// use it in an elevated or non-elevated context.
/// </summary>
public class ConfigurationFileHelper
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ConfigurationFileHelper));

    private const string PowerShellHandlerIdentifier = "pwsh";

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

    private static async Task InstallOrUpdateAppInstallerIfNeeded(IProgress<DevSetupEngineTypes.IConfigurationSetChangeData> progress)
    {
        const string AppInstallerPackageName = "Microsoft.DesktopAppInstaller";
        const string AppInstallerPackageFamilyName = "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe";
        const string AppInstallerStoreId = "9NBLGGH4NNS1";

        var doInstall = false;
        Windows.Management.Deployment.PackageManager packageManager = new();
        var currentInstallerPackage = packageManager.FindPackagesForUser(string.Empty, AppInstallerPackageFamilyName).FirstOrDefault();
        if (currentInstallerPackage == null)
        {
            doInstall = true;
        }
        else
        {
            if (!IsAppInstallerUpdateNeeded(currentInstallerPackage))
            {
                return;
            }
        }

        AppInstallManager installManager = new();
        var progressWatcher = new ApplyConfigurationProgressWatcher(progress);
        var configurationSetChangeData = GetConfigurationSetChangeData(AppInstallerPackageName, DevSetupEngineTypes.ConfigurationUnitState.Pending, string.Empty);
        progressWatcher.Report(configurationSetChangeData);

        AppInstallItem? installItem;
        if (doInstall)
        {
            installItem = await installManager.StartAppInstallAsync(AppInstallerStoreId, null, true, false);
        }
        else
        {
            installItem = await installManager.UpdateAppByPackageFamilyNameAsync(AppInstallerPackageFamilyName);
        }

        if (installItem == null)
        {
            throw new PackageOperationException(PackageOperationException.ErrorCode.DevSetupErrorUpdateNotApplicable, $"Failed to search for {AppInstallerPackageName} updates");
        }

        CancellationTokenSource cancellationToken = new();
        TypedEventHandler<AppInstallItem, object> completedHandler = (sender, args) =>
        {
            configurationSetChangeData = GetConfigurationSetChangeData(AppInstallerPackageName, DevSetupEngineTypes.ConfigurationUnitState.Completed);
            progressWatcher.Report(configurationSetChangeData);
            cancellationToken.Cancel();
        };

        TypedEventHandler<AppInstallItem, object> statusChangedHandler = (sender, args) =>
        {
            configurationSetChangeData = GetConfigurationSetChangeData(AppInstallerPackageName, DevSetupEngineTypes.ConfigurationUnitState.InProgress);
            progressWatcher.Report(configurationSetChangeData);
        };

        try
        {
            installItem.InstallInProgressToastNotificationMode = AppInstallationToastNotificationMode.NoToast;
            installItem.CompletedInstallToastNotificationMode = AppInstallationToastNotificationMode.NoToast;
            installItem.Completed += completedHandler;
            installItem.StatusChanged += statusChangedHandler;

#if DEBUG
            // In debug mode report more often and with extended description.
            var installStatus = installItem.GetCurrentStatus();
            cancellationToken.CancelAfter(TimeSpan.FromMinutes(15));
            while (!installStatus.ReadyForLaunch &&
                   !cancellationToken.IsCancellationRequested &&
                   (installStatus.InstallState != AppInstallState.Error) &&
                   (installStatus.InstallState != AppInstallState.Canceled))
            {
                installStatus = installItem.GetCurrentStatus();
                var description = GetInstallStatusDescription(installStatus);
                configurationSetChangeData = GetConfigurationSetChangeData(AppInstallerPackageName, DevSetupEngineTypes.ConfigurationUnitState.InProgress, description);
                progressWatcher.Report(configurationSetChangeData);
                await Task.Delay(5000);
                installStatus = installItem.GetCurrentStatus();
            }
#else
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(15), cancellationToken.Token);
            }
            catch (TaskCanceledException)
            {
                // Expected
            }

            var installStatus = installItem.GetCurrentStatus();
            var log = Log.ForContext("SourceContext", nameof(ConfigurationFileHelper));
            log.Information($"{AppInstallerPackageName} installation status: {installStatus}");
            if (installStatus.InstallState != AppInstallState.Completed)
            {
                throw new PackageOperationException(PackageOperationException.ErrorCode.DevSetupErrorMsStoreInstallFailed, $"Failed to install {AppInstallerPackageName} updates");
            }
#endif
        }
        finally
        {
            installItem.Completed -= completedHandler;
            installItem.StatusChanged -= statusChangedHandler;
        }
    }

    /// <summary>
    /// Open configuration set from the provided <paramref name="content"/>.
    /// </summary>
    /// <param name="content">DSC configuration file content</param>
    private async Task<ConfigurationResultTypes.OpenConfigurationSetResult> OpenConfigurationSet(string content)
    {
        try
        {
            ConfigurationStaticFunctions config = new();
            var factory = await config.CreateConfigurationSetProcessorFactoryAsync(PowerShellHandlerIdentifier).AsTask();

            // Create and configure the configuration processor.
            var processor = config.CreateConfigurationProcessor(factory);
            processor.Caller = nameof(DevSetupEngine);
            processor.Diagnostics += (sender, args) => LogConfigurationDiagnostics(args);
            processor.MinimumLevel = DiagnosticLevel.Verbose;
            _processor = processor;

            var inputStream = StringToStream(content);
            var openResult = _processor.OpenConfigurationSet(inputStream);
            _configSet = openResult.Set ?? throw new OpenConfigurationSetException(openResult);

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
        try
        {
            if (_processor == null || _configSet == null)
            {
                throw new InvalidOperationException();
            }

            _log.Information("Starting to apply configuration set");
            var applySetOperation = _processor.ApplySetAsync(_configSet, WinGet.ApplyConfigurationSetFlags.None);
            var progressWatcher = new ApplyConfigurationProgressWatcher(progress);
            applySetOperation.Progress += progressWatcher.Watcher;
            var result = await applySetOperation;

            _log.Information($"Apply configuration finished. HResult: {result.ResultCode?.HResult}");

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
        catch (Exception ex)
        {
            return new ConfigurationResultTypes.ApplyConfigurationSetResult(ex, null);
        }
    }

    public async Task<DevSetupEngineTypes.IApplyConfigurationResult> ApplyConfigurationAsync(string content, IProgress<DevSetupEngineTypes.IConfigurationSetChangeData> progress)
    {
        DevSetupEngineTypes.IOpenConfigurationSetResult? openConfigurationSetResult = default;
        DevSetupEngineTypes.IApplyConfigurationSetResult? applyConfigurationResult = default;
        try
        {
            await InstallOrUpdateAppInstallerIfNeeded(progress);

            openConfigurationSetResult = await OpenConfigurationSet(content);
            if (openConfigurationSetResult.ResultCode != null)
            {
                return new ConfigurationResultTypes.ApplyConfigurationResult(openConfigurationSetResult.ResultCode, string.Empty, openConfigurationSetResult, null);
            }

            var applyConfigurationSetResult = await ApplyConfigurationSetAsync(progress);

            return new ConfigurationResultTypes.ApplyConfigurationResult(applyConfigurationSetResult.ResultCode, string.Empty, openConfigurationSetResult, applyConfigurationSetResult);
        }
        catch (Exception ex)
        {
            return new ConfigurationResultTypes.ApplyConfigurationResult(ex, ex.Message, openConfigurationSetResult, applyConfigurationResult);
        }
    }

    private void LogConfigurationDiagnostics(WinGet.IDiagnosticInformation diagnosticInformation)
    {
        _log.Information($"WinGet: {diagnosticInformation.Message}");

        switch (diagnosticInformation.Level)
        {
            case WinGet.DiagnosticLevel.Warning:
                _log.Warning(diagnosticInformation.Message);
                return;
            case WinGet.DiagnosticLevel.Error:
                _log.Error(diagnosticInformation.Message);
                return;
            case WinGet.DiagnosticLevel.Critical:
                _log.Fatal(diagnosticInformation.Message);
                return;
            case WinGet.DiagnosticLevel.Verbose:
            case WinGet.DiagnosticLevel.Informational:
            default:
                _log.Information(diagnosticInformation.Message);
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

    /// <summary>
    /// Check if the App Installer needs to be updated.
    /// </summary>
    /// <param name="currentInstallerPackage">Microsoft.DesktopAppInstaller package</param>
    /// <returns>true if current Microsoft.DesktopAppInstaller package version is less than 1.22.10661.0</returns>
    private static bool IsAppInstallerUpdateNeeded(Package currentInstallerPackage)
    {
        var packageVersion = currentInstallerPackage.Id.Version;
        const int minMajor = 1;
        const int minMinor = 22;
        const int minBuild = 10661;

        if (packageVersion.Major > minMajor)
        {
            return false;
        }
        else if (packageVersion.Major < minMajor)
        {
            return true;
        }

        if (packageVersion.Minor > minMinor)
        {
            return false;
        }
        else if (packageVersion.Minor < minMinor)
        {
            return true;
        }

        if (packageVersion.Build > minBuild)
        {
            return false;
        }
        else if (packageVersion.Build < minBuild)
        {
            return true;
        }

        return false;
    }

    private static ConfigurationResultTypes.ConfigurationSetChangeData GetConfigurationSetChangeData(string identifier, DevSetupEngineTypes.ConfigurationUnitState unitState, string description = "")
    {
        var resultInfo = new ConfigurationResultTypes.ConfigurationUnitResultInformation(
                null,
                description,
                string.Empty,
                DevSetupEngineTypes.ConfigurationUnitResultSource.Precondition);

        var configurationUnit = new ConfigurationResultTypes.ConfigurationUnit(
            string.Empty,
            identifier,
            unitState,
            false,
            null,
            null,
            DevSetupEngineTypes.ConfigurationUnitIntent.Apply);

        return new ConfigurationResultTypes.ConfigurationSetChangeData(
            DevSetupEngineTypes.ConfigurationSetChangeEventType.UnitStateChanged,
            DevSetupEngineTypes.ConfigurationSetState.Unknown,
            unitState,
            resultInfo,
            configurationUnit);
    }

#if DEBUG
    private static string GetInstallStatusDescription(AppInstallStatus installStatus)
    {
        switch (installStatus.InstallState)
        {
            case AppInstallState.Completed:
                return "Status: Completed";
            case AppInstallState.ReadyToDownload:
                return "Status: Ready To Download";
            case AppInstallState.Starting:
                return "Status: Starting";
            case AppInstallState.Installing:
                return $"Status: Installing ({installStatus.PercentComplete}%)";
            case AppInstallState.Downloading:
                return $"Status: Downloading ({installStatus.PercentComplete}%)";
            case AppInstallState.AcquiringLicense:
                return "Status: Acquiring license";
            case AppInstallState.Pending:
                return "Status: Pending";
            case AppInstallState.RestoringData:
                return "Status: Restoring Data";
            case AppInstallState.Paused:
                return "Status: Paused";
            case AppInstallState.Error:
                return "Status: Error";
            case AppInstallState.Canceled:
                return "Status: Canceled";
            case AppInstallState.PausedLowBattery:
                return "Status: Paused low battery";
            case AppInstallState.PausedWiFiRecommended:
                return "Status: Paused WiFi Recommended";
            case AppInstallState.PausedWiFiRequired:
                return "Status: Paused WiFi Required";
            default:
                return "Status: Unknown";
        }
    }
#endif
}
