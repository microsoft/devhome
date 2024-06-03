// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DevHome.Common.Exceptions;
using DevHome.Common.Services;
using DevHome.Services;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using Microsoft.Management.Configuration;
using Microsoft.Management.Deployment;
using Serilog;

namespace DevHome.SetupFlow.Services.WinGet;

internal sealed class WinGetDeployment : IWinGetDeployment
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WinGetDeployment));
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
                _log.Information($"Attempting to create a dummy out-of-proc {nameof(PackageManager)} COM object to test if the COM server is available");
                _wingetFactory.CreatePackageManager();
                _log.Information($"WinGet COM Server is available");
            });

            return true;
        }
        catch (Exception e)
        {
            _log.Error(e, $"Failed to create dummy {nameof(PackageManager)} COM object. WinGet COM Server is not available.");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsUpdateAvailableAsync()
    {
        try
        {
            _log.Information("Checking if AppInstaller has an update ...");
            var appInstallerUpdateAvailable = await _appInstallManagerService.IsAppUpdateAvailableAsync(AppInstallerProductId);
            _log.Information($"AppInstaller update available = {appInstallerUpdateAvailable}");
            return appInstallerUpdateAvailable;
        }
        catch (Exception e)
        {
            _log.Error(e, "Failed to check if AppInstaller has an update, defaulting to false");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RegisterAppInstallerAsync()
    {
        try
        {
            _log.Information("Starting AppInstaller registration ...");
            await _packageDeploymentService.RegisterPackageForCurrentUserAsync(AppInstallerPackageFamilyName);
            _log.Information($"AppInstaller registered successfully");
            return true;
        }
        catch (RegisterPackageException e)
        {
            _log.Error(e, $"Failed to register AppInstaller");
            return false;
        }
        catch (Exception e)
        {
            _log.Error(e, "An unexpected error occurred when registering AppInstaller");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsConfigurationUnstubbedAsync()
    {
        try
        {
            return await Task.Run(() => new ConfigurationStaticFunctions().IsConfigurationAvailable);
        }
        catch (Exception e)
        {
            _log.Error(e, "An unexpected error occurred when checking if configuration is unstubbed");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UnstubConfigurationAsync()
    {
        try
        {
            _log.Information("Starting to unstub configuration ...");
            await new ConfigurationStaticFunctions().EnsureConfigurationAvailableAsync();
            _log.Information("Configuration unstubbed successfully");
            return true;
        }
        catch (Exception e)
        {
            _log.Error(e, "An unexpected error occurred when unstubbing configuration");
            return false;
        }
    }
}
