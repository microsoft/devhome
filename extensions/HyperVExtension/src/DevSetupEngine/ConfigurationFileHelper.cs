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
    private const string PowerShellHandlerIdentifierWithSecurityContext = "{73fea39f-6f4a-41c9-ba94-6fd14d633e40}";

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
    private PackageVersion _appInstallerVersion = new(0, 0, 0, 0);

    public ConfigurationFileHelper()
    {
    }

    private static PackageVersion GetAppInstallerVersion()
    {
        const string AppInstallerPackageFamilyName = "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe";

        Windows.Management.Deployment.PackageManager packageManager = new();
        var currentInstallerPackage = packageManager.FindPackagesForUser(string.Empty, AppInstallerPackageFamilyName).FirstOrDefault();
        if (currentInstallerPackage == null)
        {
            return new PackageVersion(0, 0, 0, 0);
        }

        return currentInstallerPackage.Id.Version;
    }

    private async Task InstallOrUpdateAppInstallerIfNeeded(IProgress<DevSetupEngineTypes.IConfigurationSetChangeData> progress)
    {
        const string AppInstallerPackageName = "Microsoft.DesktopAppInstaller";
        const string AppInstallerPackageFamilyName = "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe";
        const string AppInstallerStoreId = "9NBLGGH4NNS1";

        var doInstall = false;
        _appInstallerVersion = GetAppInstallerVersion();

        if (_appInstallerVersion == new PackageVersion(0, 0, 0, 0))
        {
            doInstall = true;
        }
        else if (!IsAppInstallerUpdateNeeded(_appInstallerVersion))
        {
            return;
        }

        AppInstallManager installManager = new();
        var progressWatcher = new ApplyConfigurationProgressWatcher(progress);
        var configurationSetChangeData = GetConfigurationSetChangeData(AppInstallerPackageName, DevSetupEngineTypes.ConfigurationUnitState.Pending, string.Empty);
        progressWatcher.Report(configurationSetChangeData);

        AppInstallItem? installItem;
        if (doInstall)
        {
            _log.Information($"Installing {AppInstallerPackageName}");
            installItem = await installManager.StartAppInstallAsync(AppInstallerStoreId, null, true, false);
        }
        else
        {
            _log.Information($"Updating {AppInstallerPackageName} from version {_appInstallerVersion.Major}.{_appInstallerVersion.Minor}.{_appInstallerVersion.Revision}.{_appInstallerVersion.Build}.");
            installItem = await installManager.UpdateAppByPackageFamilyNameAsync(AppInstallerPackageFamilyName);
        }

        if (installItem == null)
        {
            throw new PackageOperationException(PackageOperationException.ErrorCode.DevSetupErrorUpdateNotApplicable, $"Failed to search for {AppInstallerPackageName} updates");
        }

        CancellationTokenSource cancellationToken = new(TimeSpan.FromMinutes(15));
        TypedEventHandler<AppInstallItem, object> completedHandler = (sender, args) =>
        {
            cancellationToken.Cancel();
            _log.Information($"Completed {AppInstallerPackageName} update.");
        };

        var lastStatusReportTime = DateTime.MinValue;
        TypedEventHandler<AppInstallItem, object> statusChangedHandler = (sender, args) =>
        {
            try
            {
                var installStatus = installItem.GetCurrentStatus();
                _log.Information(GetInstallStatusDescription(installStatus));

                // AppInstallManager can report progress too often (for example reporting downloading progress every few percents),
                // so we limit the frequency of progress reports not more often than once per minute.
                if ((DateTime.Now - lastStatusReportTime) > TimeSpan.FromMinutes(1))
                {
                    lastStatusReportTime = DateTime.Now;
                    configurationSetChangeData = GetConfigurationSetChangeData(AppInstallerPackageName, DevSetupEngineTypes.ConfigurationUnitState.InProgress);
                    progressWatcher.Report(configurationSetChangeData);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to report {AppInstallerPackageName} update progress");
            }
        };

        try
        {
            installItem.InstallInProgressToastNotificationMode = AppInstallationToastNotificationMode.NoToast;
            installItem.CompletedInstallToastNotificationMode = AppInstallationToastNotificationMode.NoToast;
            installItem.Completed += completedHandler;
            installItem.StatusChanged += statusChangedHandler;

            // Wait for the updated version of the App Installer for 15 minutes (cancellation token timeout).
            // It looks like AppInstallManager doesn't handle well a race when someone else is installing the same app package
            // at the same time by not sending the completion event, so we get stuck waiting for 15 minutes and then fail.
            // To mitigate that we'll check the package version in the waiting loop below periodically and stop waiting
            // if package was update. Then report the completion to the caller if either the package was updated or we
            // received AppInstallState.Completed status.
            while (!cancellationToken.IsCancellationRequested)
            {
                _appInstallerVersion = GetAppInstallerVersion();
                _log.Information($"Current {AppInstallerPackageName} version: {_appInstallerVersion.Major}.{_appInstallerVersion.Minor}.{_appInstallerVersion.Revision}.{_appInstallerVersion.Build}.");

                if (!IsAppInstallerUpdateNeeded(_appInstallerVersion))
                {
                    installItem.Completed -= completedHandler;
                    installItem.StatusChanged -= statusChangedHandler;
                    cancellationToken.Cancel();
                    _log.Information($"Detected new {AppInstallerPackageName} version.");
                }

                cancellationToken.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(30));
            }

            var installStatus = installItem.GetCurrentStatus();
            _appInstallerVersion = GetAppInstallerVersion();
            _log.Information($"{AppInstallerPackageName} installation status: {installStatus.InstallState}");
            _log.Information($"New {AppInstallerPackageName} version: {_appInstallerVersion.Major}.{_appInstallerVersion.Minor}.{_appInstallerVersion.Revision}.{_appInstallerVersion.Build}.");
            if (!IsAppInstallerUpdateNeeded(_appInstallerVersion))
            {
                configurationSetChangeData = GetConfigurationSetChangeData(AppInstallerPackageName, DevSetupEngineTypes.ConfigurationUnitState.Completed);
                progressWatcher.Report(configurationSetChangeData);
            }
            else
            {
                throw new PackageOperationException(PackageOperationException.ErrorCode.DevSetupErrorMsStoreInstallFailed, $"Failed to install {AppInstallerPackageName}");
            }
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
            string powerShellHandlerIdentifier;
            if (IsSecurityContextSupported(_appInstallerVersion))
            {
                powerShellHandlerIdentifier = PowerShellHandlerIdentifierWithSecurityContext;
            }
            else
            {
                powerShellHandlerIdentifier = PowerShellHandlerIdentifier;
            }

            var factory = await config.CreateConfigurationSetProcessorFactoryAsync(powerShellHandlerIdentifier).AsTask();

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

            _log.Information($"Apply configuration finished. HResult: 0x{result.ResultCode?.HResult:X}");

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
    /// <param name="currentInstallerPackageVersion">Package version</param>
    /// <returns>true if current Microsoft.DesktopAppInstaller package version is less than 1.22.10661.0</returns>
    private static bool IsAppInstallerUpdateNeeded(PackageVersion currentInstallerPackageVersion)
    {
        return currentInstallerPackageVersion.LessThan(1, 22, 10661, 0);
    }

    private static bool IsSecurityContextSupported(PackageVersion currentInstallerPackageVersion)
    {
        return !currentInstallerPackageVersion.LessThan(1, 23, 1174, 0);
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
}
