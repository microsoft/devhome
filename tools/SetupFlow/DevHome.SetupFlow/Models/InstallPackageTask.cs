// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

extern alias Projection;

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using DevHome.TelemetryEvents;
using Microsoft.Management.Deployment;
using Projection::DevHome.SetupFlow.ElevatedComponent;
using Windows.Foundation;

namespace DevHome.SetupFlow.Models;

public class InstallPackageTask : ISetupTask
{
    private static readonly string MSStoreCatalogId = "StoreEdgeFD";

    private readonly IWindowsPackageManager _wpm;
    private readonly WinGetPackage _package;
    private readonly ISetupFlowStringResource _stringResource;
    private readonly WindowsPackageManagerFactory _wingetFactory;
    private readonly Lazy<bool> _requiresElevation;

    private InstallPackageException _installPackageException;

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
            PrimaryMessage = _stringResource.GetLocalized(StringResourceKey.InstallPackageError, _package.Name),
        };
    }

    public ActionCenterMessages GetRebootMessage()
    {
        return new ()
        {
            PrimaryMessage = _stringResource.GetLocalized(StringResourceKey.InstalledPackageReboot, _package.Name),
        };
    }

    IAsyncOperation<TaskFinishedState> ISetupTask.Execute()
    {
        ReportAppSelectedForInstallEvent();
        return Task.Run(async () =>
        {
            try
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Starting installation of package {_package.Id}");
                var installResult = await _wpm.InstallPackageAsync(_package);
                RequiresReboot = installResult.RebootRequired;
                WasInstallSuccessful = true;

                ReportAppInstallSucceededEvent();
                return TaskFinishedState.Success;
            }
            catch (InstallPackageException e)
            {
                _installPackageException = e;
                ReportAppInstallFailedEvent();
                Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to install package with status {e.Status} and installer error code {e.InstallerErrorCode}");
                return TaskFinishedState.Failure;
            }
            catch (Exception e)
            {
                ReportAppInstallFailedEvent();
                Log.Logger?.ReportError(Log.Component.AppManagement, $"Exception thrown while installing package: {e.Message}");
                return TaskFinishedState.Failure;
            }
        }).AsAsyncOperation();
    }

    IAsyncOperation<TaskFinishedState> ISetupTask.ExecuteAsAdmin(IElevatedComponentFactory elevatedComponentFactory)
    {
        ReportAppSelectedForInstallEvent();
        return Task.Run(async () =>
        {
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Starting installation with elevation of package {_package.Id}");
            var elevatedTask = elevatedComponentFactory.CreateElevatedInstallTask();
            var elevatedResult = await elevatedTask.InstallPackage(_package.Id, _package.CatalogName);
            WasInstallSuccessful = elevatedResult.TaskSucceeded;
            RequiresReboot = elevatedResult.RebootRequired;

            if (elevatedResult.TaskSucceeded)
            {
                ReportAppInstallSucceededEvent();
                return TaskFinishedState.Success;
            }
            else
            {
                ReportAppInstallFailedEvent();
                return TaskFinishedState.Failure;
            }
        }).AsAsyncOperation();
    }

    private string GetErrorReason()
    {
        return _installPackageException?.Status switch
        {
            InstallResultStatus.BlockedByPolicy =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorBlockedByPolicy),
            InstallResultStatus.InternalError =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorInternalError),
            InstallResultStatus.DownloadError =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorDownloadError),
            InstallResultStatus.InstallError =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorInstallError, $"0x{_installPackageException.InstallerErrorCode:X}"),
            InstallResultStatus.NoApplicableInstallers =>
                _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorNoApplicableInstallers),
            _ => _stringResource.GetLocalized(StringResourceKey.InstallPackageErrorUnknownError),
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
