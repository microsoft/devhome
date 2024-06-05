// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Extensions;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Models;
using Microsoft.Management.Deployment;
using Serilog;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.Services.WinGet;

/// <summary>
/// Installs a package using the Windows Package Manager (WinGet).
/// </summary>
internal sealed class WinGetPackageInstaller : IWinGetPackageInstaller
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WinGetPackageInstaller));
    private readonly WindowsPackageManagerFactory _wingetFactory;
    private readonly IWinGetPackageFinder _packageFinder;

    public WinGetPackageInstaller(WindowsPackageManagerFactory wingetFactory, IWinGetPackageFinder packageFinder)
    {
        _wingetFactory = wingetFactory;
        _packageFinder = packageFinder;
    }

    /// <inheritdoc />
    public async Task<InstallPackageResult> InstallPackageAsync(WinGetCatalog catalog, string packageId, string version = null)
    {
        if (catalog == null)
        {
            throw new CatalogNotInitializedException();
        }

        // 1. Find package
        var package = await _packageFinder.GetPackageAsync(catalog, packageId);
        if (package == null)
        {
            _log.Error($"Install aborted for package {packageId} because it was not found in the provided catalog {catalog.GetDescriptiveName()}");
            throw new FindPackagesException(FindPackagesResultStatus.CatalogError);
        }

        // 2. Install package
        _log.Information($"Starting package installation for {packageId} from catalog {catalog.GetDescriptiveName()}");
        var installResult = await InstallPackageInternalAsync(package, version);
        var extendedErrorCode = installResult.ExtendedErrorCode?.HResult ?? HRESULT.S_OK;
        var installErrorCode = installResult.GetValueOrDefault(res => res.InstallerErrorCode, HRESULT.S_OK); // WPM API V4

        // 3. Report install result
        _log.Information($"Install result: Status={installResult.Status}, InstallerErrorCode={installErrorCode}, ExtendedErrorCode={extendedErrorCode}, RebootRequired={installResult.RebootRequired}");
        if (installResult.Status != InstallResultStatus.Ok)
        {
            throw new InstallPackageException(installResult.Status, extendedErrorCode, installErrorCode);
        }

        _log.Information($"Completed package installation for {packageId} from catalog {catalog.GetDescriptiveName()}");
        return new InstallPackageResult()
        {
            ExtendedErrorCode = extendedErrorCode,
            RebootRequired = installResult.RebootRequired,
        };
    }

    /// <summary>
    /// Install a package from a catalog.
    /// </summary>
    /// <param name="package">Package to install</param>
    /// <returns>Install result</returns>
    private async Task<InstallResult> InstallPackageInternalAsync(CatalogPackage package, string version = null)
    {
        var installOptions = _wingetFactory.CreateInstallOptions();
        installOptions.PackageInstallMode = PackageInstallMode.Silent;
        if (!string.IsNullOrWhiteSpace(version))
        {
            installOptions.PackageVersionId = FindVersionOrThrow(package, version);
        }
        else
        {
            _log.Information($"Install version not specified. Falling back to default install version {package.DefaultInstallVersion.Version}");
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

        _log.Error($"Specified install version was not found {version}.");
        throw new InstallPackageException(InstallResultStatus.InvalidOptions, InstallPackageException.InstallErrorInvalidParameter, HRESULT.S_OK);
    }
}
