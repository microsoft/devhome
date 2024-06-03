// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DevHome.Services.Core.Contracts;
using DevHome.Services.Core.Exceptions;
using DevHome.Services.WindowsPackageManager.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Management.Configuration;
using Microsoft.Management.Deployment;

namespace DevHome.Services.WindowsPackageManager.Services;

internal sealed class WinGetDeployment : IWinGetDeployment
{
    private readonly ILogger _logger;
    private readonly WindowsPackageManagerFactory _wingetFactory;
    private readonly IMicrosoftStoreService _msStoreService;
    private readonly IPackageDeploymentService _packageDeploymentService;

    // App installer constants
    public const int AppInstallerErrorFacility = 0xA15;
    public const string AppInstallerProductId = "9NBLGGH4NNS1";
    public const string AppInstallerPackageFamilyName = "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe";

    public WinGetDeployment(
        ILogger<WinGetDeployment> logger,
        WindowsPackageManagerFactory wingetFactory,
        IMicrosoftStoreService msStoreService,
        IPackageDeploymentService packageDeploymentService)
    {
        _logger = logger;
        _wingetFactory = wingetFactory;
        _msStoreService = msStoreService;
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
                _logger.LogInformation($"Attempting to create a dummy out-of-proc {nameof(PackageManager)} COM object to test if the COM server is available");
                _wingetFactory.CreatePackageManager();
                _logger.LogInformation($"WinGet COM Server is available");
            });

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to create dummy {nameof(PackageManager)} COM object. WinGet COM Server is not available.");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsUpdateAvailableAsync()
    {
        try
        {
            _logger.LogInformation("Checking if AppInstaller has an update ...");
            var appInstallerUpdateAvailable = await _msStoreService.IsAppUpdateAvailableAsync(AppInstallerProductId);
            _logger.LogInformation($"AppInstaller update available = {appInstallerUpdateAvailable}");
            return appInstallerUpdateAvailable;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to check if AppInstaller has an update, defaulting to false");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RegisterAppInstallerAsync()
    {
        try
        {
            _logger.LogInformation("Starting AppInstaller registration ...");
            await _packageDeploymentService.RegisterPackageForCurrentUserAsync(AppInstallerPackageFamilyName);
            _logger.LogInformation($"AppInstaller registered successfully");
            return true;
        }
        catch (RegisterPackageException e)
        {
            _logger.LogError(e, $"Failed to register AppInstaller");
            return false;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An unexpected error occurred when registering AppInstaller");
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
            _logger.LogError(e, "An unexpected error occurred when checking if configuration is unstubbed");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UnstubConfigurationAsync()
    {
        try
        {
            _logger.LogInformation("Starting to unstub configuration ...");
            await new ConfigurationStaticFunctions().EnsureConfigurationAvailableAsync();
            _logger.LogInformation("Configuration unstubbed successfully");
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An unexpected error occurred when unstubbing configuration");
            return false;
        }
    }
}
