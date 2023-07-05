// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.Contract.TaskOperator;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskOperator;
using DevHome.Telemetry;
using Microsoft.Management.Deployment;
using Windows.Foundation;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.Models;

public class InstallPackageTask : ISetupTask
{
    private static readonly string MSStoreCatalogId = "StoreEdgeFD";

    private readonly IWindowsPackageManager _wpm;
    private readonly WinGetPackage _package;
    private readonly ISetupFlowStringResource _stringResource;
    private readonly WindowsPackageManagerFactory _wingetFactory;
    private readonly Lazy<bool> _requiresElevation;

    private InstallResultStatus _installResultStatus;
    private uint _installerErrorCode;
    private int _extendedErrorCode;

    public bool RequiresAdmin => _requiresElevation.Value;

    public bool IsFromMSStore => string.Equals(_package.CatalogId, MSStoreCatalogId, StringComparison.Ordinal);

    // We don't have this information available for each package before
    // installation in the WinGet COM API, but we do get it after installation.
    public bool RequiresReboot { get; set; }

    // May potentially be moved to a central list in the future.
    public bool WasInstallSuccessful
    {
        get; private set;
    }

    public bool DependsOnDevDriveToBeInstalled
    {
        get;
    }

    public InstallPackageTask(
        IWindowsPackageManager wpm,
        ISetupFlowStringResource stringResource,
        WindowsPackageManagerFactory wingetFactory,
        WinGetPackage package)
    {
        _wpm = wpm;
        _stringResource = stringResource;
        _wingetFactory = wingetFactory;
        _package = package;
        _requiresElevation = new (RequiresElevation);
    }

    public TaskMessages GetLoadingMessages()
    {
        return new TaskMessages
        {
            Executing = _stringResource.GetLocalized(StringResourceKey.InstallingPackage, _package.Name),
            Error = _stringResource.GetLocalized(StringResourceKey.InstallPackageError, _package.Name),
            Finished = _stringResource.GetLocalized(StringResourceKey.InstalledPackage, _package.Name),
            NeedsReboot = _stringResource.GetLocalized(StringResourceKey.InstalledPackageReboot, _package.Name),
        };
    }

    public ActionCenterMessages GetErrorMessages()
    {
        return new ()
        {
            PrimaryMessage = GetInstallResultMessage(),
        };
    }

    public ActionCenterMessages GetRebootMessage()
    {
        return new ()
        {
            PrimaryMessage = _extendedErrorCode == HRESULT.S_OK ?
                _stringResource.GetLocalized(StringResourceKey.InstalledPackageReboot, _package.Name) :
                GetExtendedErrorCodeMessage(),
        };
    }

    public IAsyncOperation<TaskFinishedState> Execute(ITaskOperatorFactory operatorFactory)
    {
        ReportAppSelectedForInstallEvent();
        return Task.Run(async () =>
        {
            try
            {
                // TODO: Run installation in a separate non-elevated process
                // instead of the same Dev Home process. This ensures that even if
                // Dev Home is launched as admin, installation will still run in a
                // non-elevated process.
                // https://github.com/microsoft/devhome/issues/1251

                // Since Execute method runs in the same process as Dev Home,
                // we can use the previously obtained catalog package.
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Starting installation of package {_package.Id}");
                var installOperator = (InstallOperator)operatorFactory.CreateInstallPackageOperator();
                return await InstallAsync(async () => await installOperator.InstallPackageAsync(_package));
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.Configuration, $"Failed to execute {nameof(InstallPackageTask)}", e);
                return TaskFinishedState.Failure;
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<TaskFinishedState> ExecuteAsAdmin(ITaskOperatorFactory elevatedOperatorFactory)
    {
        ReportAppSelectedForInstallEvent();
        return Task.Run(async () =>
        {
            try
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Starting elevated installation of package {_package.Id}");
                var installOperator = elevatedOperatorFactory.CreateInstallPackageOperator();
                return await InstallAsync(async () => await installOperator.InstallPackageAsync(_package.Id, _package.CatalogName));
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.Configuration, $"Failed to execute {nameof(InstallPackageTask)} in elevated process", e);
                return TaskFinishedState.Failure;
            }
        }).AsAsyncOperation();
    }

    private async Task<TaskFinishedState> InstallAsync(Func<Task<IInstallPackageResult>> installFunction)
    {
        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Starting installation with elevation of package {_package.Id}");
        var result = await installFunction();
        WasInstallSuccessful = result.Succeeded;
        RequiresReboot = result.RebootRequired;
        _installResultStatus = (InstallResultStatus)result.Status;
        _extendedErrorCode = result.ExtendedErrorCode;
        _installerErrorCode = result.InstallerErrorCode;

        if (result.Succeeded)
        {
            ReportAppInstallSucceededEvent();
            return TaskFinishedState.Success;
        }

        ReportAppInstallFailedEvent();
        return TaskFinishedState.Failure;
    }

    private string GetInstallResultMessage()
    {
        var packageName = _package.Name;
        return _installResultStatus switch
        {
            InstallResultStatus.BlockedByPolicy =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorBlockedByPolicy, packageName),
            InstallResultStatus.InternalError =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorInternalError, packageName),
            InstallResultStatus.DownloadError =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorDownloadError, packageName),
            InstallResultStatus.InstallError =>
                GetExtendedErrorCodeMessage(),
            InstallResultStatus.NoApplicableInstallers =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorNoApplicableInstallers, packageName),
            _ => _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorUnknownErrorWithErrorCode, packageName, $"0x{_extendedErrorCode:X}"),
        };
    }

    /// <summary>
    /// Extracts the facility of the specified HRESULT
    /// </summary>
    /// <param name="hr">The HRESULT value</param>
    /// <returns>Facility of the specified HRESULT</returns>
    /// <remarks>https://learn.microsoft.com/windows/win32/api/winerror/nf-winerror-hresult_facility</remarks>
    private int HResultFacility(int hr) => (hr >> 16) & 0x1FFF;

    public bool IsAppInstallerErrorFacility(int hr) => HResultFacility(hr) == WindowsPackageManager.AppInstallerErrorFacility;

    private string GetExtendedErrorCodeMessage()
    {
        var packageName = _package.Name;
        if (!IsAppInstallerErrorFacility(_extendedErrorCode))
        {
            var errorMessage = _stringResource.GetLocalizedErrorMsg(_extendedErrorCode, Log.Component.AppManagement);
            return _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessageSystemMessage, packageName, errorMessage);
        }

        return _extendedErrorCode switch
        {
            InstallPackageException.InstallErrorPackageInUse =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessagePackageInUse, packageName),
            InstallPackageException.InstallErrorInstallInProgress =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessageInstallInProgress, packageName),
            InstallPackageException.InstallErrorFileInUse =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessageFileInUse, packageName),
            InstallPackageException.InstallErrorMissingDependency =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessageMissingDependency, packageName),
            InstallPackageException.InstallErrorDiskFull =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessageDiskFull, packageName),
            InstallPackageException.InstallErrorInsufficientMemory =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessageInsufficientMemory, packageName),
            InstallPackageException.InstallErrorNoNetwork =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessageNoNetwork, packageName),
            InstallPackageException.InstallErrorContactSupport =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessageContactSupport, packageName),
            InstallPackageException.InstallErrorRebootRequiredToFinish =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessageRebootRequiredToFinish, packageName),
            InstallPackageException.InstallErrorRebootRequiredToInstall =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessageRebootRequiredToInstall, packageName),
            InstallPackageException.InstallErrorRebootInitiated =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessageRebootInitiated, packageName),
            InstallPackageException.InstallErrorCancelledByUser =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessageCancelledByUser, packageName),
            InstallPackageException.InstallErrorAlreadyInstalled =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessageAlreadyInstalled, packageName),
            InstallPackageException.InstallErrorDowngrade =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessageDowngrade, packageName),
            InstallPackageException.InstallErrorBlockedByPolicy =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessageBlockedByPolicy, packageName),
            InstallPackageException.InstallErrorDependencies =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessageDependencies, packageName),
            InstallPackageException.InstallErrorPackageInUseByApplication =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessagePackageInUseByApplication, packageName),
            InstallPackageException.InstallErrorInvalidParameter =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessageInvalidParameter, packageName),
            InstallPackageException.InstallErrorSystemNotSupported =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorMessageSystemNotSupported, packageName),
            _ => _installerErrorCode == HRESULT.S_OK ?
                    _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorUnknownErrorWithErrorCode, packageName, $"0x{_extendedErrorCode:X}") :
                    _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorUnknownErrorWithErrorCodeAndExitCode, packageName, $"0x{_extendedErrorCode:X}", _installerErrorCode),
        };
    }

    private bool RequiresElevation()
    {
        var options = _wingetFactory.CreateInstallOptions();
        options.PackageInstallScope = PackageInstallScope.Any;
        return _package.RequiresElevation(options);
    }

    private void ReportAppSelectedForInstallEvent()
    {
        TelemetryFactory.Get<ITelemetry>().Log("AppInstall_AppSelected", LogLevel.Critical, new AppInstallEvent(_package.Id, _package.CatalogId));
    }

    private void ReportAppInstallSucceededEvent()
    {
        TelemetryFactory.Get<ITelemetry>().Log("AppInstall_InstallSucceeded", LogLevel.Critical, new AppInstallEvent(_package.Id, _package.CatalogId));
    }

    private void ReportAppInstallFailedEvent()
    {
        TelemetryFactory.Get<ITelemetry>().LogError("AppInstall_InstallFailed", LogLevel.Critical, new AppInstallEvent(_package.Id, _package.CatalogId));
    }
}
