// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.Common.Exceptions;
using DevHome.Common.Services;
using DevHome.Services;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using Microsoft.Management.Deployment;

namespace DevHome.SetupFlow.Services.WinGet;

internal class WinGetDeployment : IWinGetDeployment
{
    private readonly WindowsPackageManagerFactory _wingetFactory;
    private readonly IAppInstallManagerService _appInstallManagerService;
    private readonly IPackageDeploymentService _packageDeploymentService;

    // App installer constants
    public const int AppInstallerErrorFacility = 0xA15;
    public const string AppInstallerProductId = "9NBLGGH4NNS1";
    public const string AppInstallerPackageFamilyName = "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe";

    public WinGetDeployment(
        WindowsPackageManagerFactory wingetFactory,
        IAppInstallManagerService appInstallManagerService,
        IPackageDeploymentService packageDeploymentService)
    {
        _wingetFactory = wingetFactory;
        _appInstallManagerService = appInstallManagerService;
        _packageDeploymentService = packageDeploymentService;
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            // Quick check (without recovery) if the COM server is available by
            // creating a dummy out-of-proc object
            await Task.Run(() =>
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Attempting to create a dummy out-of-proc {nameof(PackageManager)} COM object to test if the COM server is available");
                _wingetFactory.CreatePackageManager();
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"WinGet COM Server is available");
            });

            return true;
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to create dummy {nameof(PackageManager)} COM object. WinGet COM Server is not available.", e);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsUpdateAvailableAsync()
    {
        try
        {
            Log.Logger?.ReportInfo(Log.Component.AppManagement, "Checking if AppInstaller has an update ...");
            var appInstallerUpdateAvailable = await _appInstallManagerService.IsAppUpdateAvailableAsync(AppInstallerProductId);
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"AppInstaller update available = {appInstallerUpdateAvailable}");
            return appInstallerUpdateAvailable;
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, "Failed to check if AppInstaller has an update, defaulting to false", e);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RegisterAppInstallerAsync()
    {
        try
        {
            Log.Logger?.ReportInfo(Log.Component.AppManagement, "Starting AppInstaller registration ...");
            await _packageDeploymentService.RegisterPackageForCurrentUserAsync(AppInstallerPackageFamilyName);
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"AppInstaller registered successfully");
            return true;
        }
        catch (RegisterPackageException e)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to register AppInstaller", e);
            return false;
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, "An unexpected error occurred when registering AppInstaller", e);
            return false;
        }
    }
}
