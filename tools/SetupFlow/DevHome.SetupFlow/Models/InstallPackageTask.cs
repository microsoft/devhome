// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

extern alias Projection;

using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DevHome.Services.WindowsPackageManager.Contracts;
using DevHome.Services.WindowsPackageManager.Exceptions;
using DevHome.SetupFlow.Common.Contracts;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using Microsoft.Management.Deployment;
using Projection::DevHome.SetupFlow.ElevatedComponent;
using Serilog;
using Windows.Foundation;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.Models;

public class InstallPackageTask : ISetupTask
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(InstallPackageTask));
    private static readonly string MSStoreCatalogId = "StoreEdgeFD";

    private readonly IWinGet _winget;
    private readonly IWinGetPackage _package;
    private readonly ISetupFlowStringResource _stringResource;
    private readonly Guid _activityId;
    private readonly string _installVersion;

    private InstallResultStatus _installResultStatus;
    private uint _installerErrorCode;
    private int _extendedErrorCode;

    public event ISetupTask.ChangeMessageHandler AddMessage;

    public bool RequiresAdmin => _package.IsElevationRequired;

    public bool IsFromMSStore => string.Equals(_package.CatalogId, MSStoreCatalogId, StringComparison.Ordinal);

    // We don't have this information available for each package before
    // installation in the WinGet COM API, but we do get it after installation.
    public bool RequiresReboot { get; set; }

    /// <summary>
    /// Gets target device name. Inherited via ISetupTask but unused.
    /// </summary>
    public string TargetName => string.Empty;

    // May potentially be moved to a central list in the future.
    public bool WasInstallSuccessful
    {
        get; private set;
    }

    public bool DependsOnDevDriveToBeInstalled
    {
        get;
    }

    public ISummaryInformationViewModel SummaryScreenInformation { get; }

    public string PackageName => _package.Name;

    public bool IsInstalled => _package.IsInstalled;

#pragma warning disable 67
    public event ISetupTask.ChangeActionCenterMessageHandler UpdateActionCenterMessage;
#pragma warning restore 67

    public InstallPackageTask(
        IWinGet winget,
        ISetupFlowStringResource stringResource,
        IWinGetPackage package,
        string installVersion,
        Guid activityId)
    {
        _winget = winget;
        _stringResource = stringResource;
        _package = package;
        _activityId = activityId;
        _installVersion = installVersion;
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
        return new()
        {
            PrimaryMessage = GetInstallResultMessage(),
        };
    }

    public ActionCenterMessages GetRebootMessage()
    {
        return new()
        {
            PrimaryMessage = _extendedErrorCode == HRESULT.S_OK ?
                _stringResource.GetLocalized(StringResourceKey.InstalledPackageReboot, _package.Name) :
                GetExtendedErrorCodeMessage(),
        };
    }

    /// <summary>
    /// Get the arguments for this task
    /// </summary>
    /// <returns>Arguments for this task</returns>
    public InstallPackageTaskArguments GetArguments()
    {
        return new InstallPackageTaskArguments
        {
            PackageId = _package.Id,
            CatalogName = _package.CatalogName,
            Version = _installVersion,
            IsElevationRequired = _package.IsElevationRequired,
        };
    }

    IAsyncOperationWithProgress<TaskFinishedState, int> ISetupTask.Execute()
    {
        return AsyncInfo.Run<TaskFinishedState, int>(async (_, progress) =>
        {
            try
            {
                _log.Information($"Starting installation of package {_package.Id}");
                AddMessage(_stringResource.GetLocalized(StringResourceKey.StartingInstallPackageMessage, _package.Id), MessageSeverityKind.Info);
                var packageUri = _package.GetUri(_installVersion);
                var install = _winget.InstallPackageAsync(packageUri, _activityId);
                install.Progress += (_, p) =>
                {
                    progress.Report((int)(p.DownloadProgress * 100));
                };
                var installResult = await install;
                RequiresReboot = installResult.RebootRequired;
                WasInstallSuccessful = true;

                // Set the extended error code in case a reboot is required
                _extendedErrorCode = installResult.ExtendedErrorCode;

                return TaskFinishedState.Success;
            }
            catch (InstallPackageException e)
            {
                _installResultStatus = e.Status;
                _extendedErrorCode = e.ExtendedErrorCode;
                _installerErrorCode = e.InstallerErrorCode;
                _log.Error($"Failed to install package with status {e.Status} and installer error code {e.InstallerErrorCode}");
                return TaskFinishedState.Failure;
            }
            catch (Exception e)
            {
                _log.Error(e, $"Exception thrown while installing package.");
                return TaskFinishedState.Failure;
            }
        });
    }

    IAsyncOperationWithProgress<TaskFinishedState, int> ISetupTask.ExecuteAsAdmin(IElevatedComponentOperation elevatedComponentOperation)
    {
        return AsyncInfo.Run<TaskFinishedState, int>(async (_, progress) =>
        {
            try
            {
                _log.Information($"Starting installation with elevation of package {_package.Id}");
                AddMessage(_stringResource.GetLocalized(StringResourceKey.StartingInstallPackageMessage, _package.Id), MessageSeverityKind.Info);
                var elevated = elevatedComponentOperation.InstallPackageAsync(_package.Id, _package.CatalogName, _installVersion, _activityId);
                elevated.Progress += (_, p) =>
                {
                    progress.Report((int)(p.Current * 100));
                };
                var elevatedResult = await elevated;
                WasInstallSuccessful = elevatedResult.TaskSucceeded;
                RequiresReboot = elevatedResult.RebootRequired;
                _installResultStatus = (InstallResultStatus)elevatedResult.Status;
                _extendedErrorCode = elevatedResult.ExtendedErrorCode;
                _installerErrorCode = elevatedResult.InstallerErrorCode;
                return elevatedResult.TaskSucceeded ? TaskFinishedState.Success : TaskFinishedState.Failure;
            }
            catch (Exception e)
            {
                _log.Error(e, $"Exception thrown while installing package.");
                return TaskFinishedState.Failure;
            }
        });
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

    public bool IsAppInstallerErrorFacility(int hr) => HResultFacility(hr) == IWinGet.AppInstallerErrorFacility;

    private string GetExtendedErrorCodeMessage()
    {
        var packageName = _package.Name;
        if (!IsAppInstallerErrorFacility(_extendedErrorCode))
        {
            var errorMessage = _stringResource.GetLocalizedErrorMsg(_extendedErrorCode, Identity.Component.AppManagement);
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
}
