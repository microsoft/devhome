// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DevHome.Services.Core.Extensions;
using DevHome.Services.WindowsPackageManager.COM;
using DevHome.Services.WindowsPackageManager.Contracts;
using DevHome.Services.WindowsPackageManager.Exceptions;
using DevHome.Services.WindowsPackageManager.Models;
using DevHome.Services.WindowsPackageManager.TelemetryEvents;
using DevHome.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Management.Deployment;
using Windows.Win32.Foundation;

namespace DevHome.Services.WindowsPackageManager.Services;

/// <summary>
/// Installs a package using the Windows Package Manager (WinGet).
/// </summary>
internal sealed class WinGetPackageInstaller : IWinGetPackageInstaller
{
    private readonly ILogger _logger;
    private readonly WindowsPackageManagerFactory _wingetFactory;
    private readonly IWinGetPackageFinder _packageFinder;

    public WinGetPackageInstaller(
        ILogger<WinGetPackageInstaller> logger,
        WindowsPackageManagerFactory wingetFactory,
        IWinGetPackageFinder packageFinder)
    {
        _logger = logger;
        _wingetFactory = wingetFactory;
        _packageFinder = packageFinder;
    }

    /// <inheritdoc />
    public async Task<IWinGetInstallPackageResult> InstallPackageAsync(WinGetCatalog catalog, string packageId, string version, Guid activityId)
    {
        if (catalog == null)
        {
            throw new CatalogNotInitializedException();
        }

        // Report telemetry for attempting app install
        TelemetryFactory.Get<ITelemetry>().Log("AppInstall_AppSelected", Telemetry.LogLevel.Critical, new AppInstallUserEvent(packageId, catalog.Catalog.Info.Id), activityId);

        try
        {
            // 1. Find package
            var package = await _packageFinder.GetPackageAsync(catalog, packageId);
            if (package == null)
            {
                _logger.LogError($"Install aborted for package {packageId} because it was not found in the provided catalog {catalog.GetDescriptiveName()}");
                throw new FindPackagesException(FindPackagesResultStatus.CatalogError);
            }

            // 2. Install package
            _logger.LogInformation($"Starting package installation for {packageId} from catalog {catalog.GetDescriptiveName()}");
            var installResult = await InstallPackageInternalAsync(package, version, activityId);
            var extendedErrorCode = installResult.ExtendedErrorCode?.HResult ?? HRESULT.S_OK;
            var installErrorCode = installResult.GetValueOrDefault(res => res.InstallerErrorCode, HRESULT.S_OK); // WPM API V4

            // 3. Report install result
            _logger.LogInformation($"Install result: Status={installResult.Status}, InstallerErrorCode={installErrorCode}, ExtendedErrorCode={extendedErrorCode}, RebootRequired={installResult.RebootRequired}");
            if (installResult.Status != InstallResultStatus.Ok)
            {
                throw new InstallPackageException(installResult.Status, extendedErrorCode, installErrorCode);
            }

            _logger.LogInformation($"Completed package installation for {packageId} from catalog {catalog.GetDescriptiveName()}");
            TelemetryFactory.Get<ITelemetry>().Log("AppInstall_InstallSucceeded", Telemetry.LogLevel.Critical, new AppInstallResultEvent(package.Id, catalog.Catalog.Info.Id), activityId);
            return new WinGetInstallPackageResult()
            {
                ExtendedErrorCode = extendedErrorCode,
                RebootRequired = installResult.RebootRequired,
            };
        }
        catch
        {
            // Report telemetry for failed install and rethrow
            TelemetryFactory.Get<ITelemetry>().LogError("AppInstall_InstallFailed", Telemetry.LogLevel.Critical, new AppInstallResultEvent(packageId, catalog.Catalog.Info.Id), activityId);
            throw;
        }
    }

    /// <summary>
    /// Install a package from a catalog.
    /// </summary>
    /// <param name="package">Package to install</param>
    /// <returns>Install result</returns>
    private async Task<InstallResult> InstallPackageInternalAsync(CatalogPackage package, string version, Guid activityId)
    {
        var installOptions = _wingetFactory.CreateInstallOptions();
        installOptions.PackageInstallMode = PackageInstallMode.Silent;
        if (!string.IsNullOrWhiteSpace(version))
        {
            installOptions.PackageVersionId = FindVersionOrThrow(package, version);
            if (installOptions.PackageVersionId.Version != package.DefaultInstallVersion.Version)
            {
                TelemetryFactory.Get<ITelemetry>().Log("AppInstall_VersionSpecified", Telemetry.LogLevel.Critical, new AppInstallUserEvent(package.Id, package.DefaultInstallVersion.PackageCatalog.Info.Id), activityId);
            }
        }
        else
        {
            _logger.LogInformation($"Install version not specified. Falling back to default install version {package.DefaultInstallVersion.Version}");
        }

        var packageManager = _wingetFactory.CreatePackageManager();
        return await packageManager.InstallPackageAsync(package, installOptions).AsTask();
    }

    /// <summary>
    /// Find a specific version in the list of available versions for a package.
    /// </summary>
    /// <param name="package">Target package</param>
    /// <param name="version">Version to find</param>
    /// <returns>Specified version</returns>
    /// <exception>Exception thrown if the specified version was not found</exception>
    private PackageVersionId FindVersionOrThrow(CatalogPackage package, string version)
    {
        // Find the version in the list of available versions
        for (var i = 0; i < package.AvailableVersions.Count; i++)
        {
            if (package.AvailableVersions[i].Version == version)
            {
                return package.AvailableVersions[i];
            }
        }

        _logger.LogError($"Specified install version was not found {version}.");
        throw new InstallPackageException(InstallResultStatus.InvalidOptions, InstallPackageException.InstallErrorInvalidParameter, HRESULT.S_OK);
    }
}
