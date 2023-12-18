// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DevHome.Common.Exceptions;
using DevHome.Common.Services;
using DevHome.Services;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services.WinGet;
using Microsoft.Management.Deployment;

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Windows package manager class is an entry point for using the WinGet COM API.
/// </summary>
public class WindowsPackageManager : IWindowsPackageManager
{
    // App installer constants
    public const int AppInstallerErrorFacility = 0xA15;
    public const string AppInstallerProductId = "9NBLGGH4NNS1";
    public const string AppInstallerPackageFamilyName = "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe";

    // COM error codes
    public const int RpcServerUnavailable = unchecked((int)0x800706BA);
    public const int RpcCallFailed = unchecked((int)0x800706BE);

    private readonly WindowsPackageManagerFactory _wingetFactory;
    private readonly IAppInstallManagerService _appInstallManagerService;
    private readonly IPackageDeploymentService _packageDeploymentService;
    private readonly IWinGetCatalogConnector _catalogConnector;
    private readonly IWinGetPackageFinder _packageFinder;
    private readonly IWinGetPackageInstaller _packageInstaller;
    private readonly IWinGetProtocolParser _protocolParser;

    public WindowsPackageManager(
        WindowsPackageManagerFactory wingetFactory,
        IWinGetCatalogConnector catalogConnector,
        IWinGetPackageFinder packageFinder,
        IWinGetPackageInstaller packageInstaller,
        IAppInstallManagerService appInstallManagerService,
        IPackageDeploymentService packageDeploymentService,
        IWinGetProtocolParser protocolParser)
    {
        _wingetFactory = wingetFactory;
        _appInstallManagerService = appInstallManagerService;
        _packageDeploymentService = packageDeploymentService;
        _catalogConnector = catalogConnector;
        _packageFinder = packageFinder;
        _packageInstaller = packageInstaller;
        _protocolParser = protocolParser;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        // Run action in a background thread to avoid blocking the UI thread
        // Async methods are blocking in WinGet: https://github.com/microsoft/winget-cli/issues/3205
        await Task.Run(async () =>
        {
            await _catalogConnector.CreateAndConnectCatalogsAsync();
        });
    }

    /// <inheritdoc/>
    public async Task<InstallPackageResult> InstallPackageAsync(IWinGetPackage package, Guid activityId)
    {
        return await DoWithRecovery(async () =>
        {
            var catalog = await _catalogConnector.GetPackageCatalogAsync(package);
            return await _packageInstaller.InstallPackageAsync(catalog, package.Id);
        });
    }

    /// <inheritdoc/>
    public async Task<IList<IWinGetPackage>> GetPackagesAsync(ISet<Uri> packageUriSet)
    {
        return await DoWithRecovery(async () =>
        {
            // 1. Group packages by their catalogs
            Dictionary<WinGetCatalog, HashSet<string>> packageIdsByCatalog = new ();
            foreach (var packageUri in packageUriSet)
            {
                var packageInfo = await _protocolParser.ParsePackageUriAsync(packageUri);
                if (packageInfo != null)
                {
                    if (!packageIdsByCatalog.ContainsKey(packageInfo.catalog))
                    {
                        packageIdsByCatalog[packageInfo.catalog] = new HashSet<string>();
                    }

                    packageIdsByCatalog[packageInfo.catalog].Add(packageInfo.packageId);
                }
                else
                {
                    Log.Logger?.ReportWarn(Log.Component.AppManagement, $"Failed to get package details from uri '{packageUri}'");
                }
            }

            // 2. Get packages from each catalog
            var result = new List<IWinGetPackage>();
            foreach (var catalog in packageIdsByCatalog.Keys)
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Getting packages from catalog {catalog.Type}");
                var packages = await _packageFinder.GetPackagesAsync(catalog, packageIdsByCatalog[catalog]);
                result.AddRange(packages.Select(p => CreateWinGetPackage(p)));
            }

            return result;
        });
    }

    /// <inheritdoc/>
    public async Task<IList<IWinGetPackage>> SearchAsync(string query, uint limit = 0)
    {
        return await DoWithRecovery(async () =>
        {
            var searchCatalog = await _catalogConnector.GetCustomSearchCatalogAsync();
            var results = await _packageFinder.SearchAsync(searchCatalog, query, limit);
            return results.Select(p => CreateWinGetPackage(p)).ToList();
        });
    }

    /// <inheritdoc/>
    public async Task<bool> CanSearchAsync()
    {
        try
        {
            // Attempt to access the catalog name to verify that the catalog's out-of-proc object is still alive
            var searchCatalog = await _catalogConnector.GetCustomSearchCatalogAsync();
            searchCatalog?.Catalog.Info.Name.ToString();
            return searchCatalog != null;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public bool IsMsStorePackage(IWinGetPackage package) => _catalogConnector.IsMsStorePackage(package);

    /// <inheritdoc/>
    public bool IsWinGetPackage(IWinGetPackage package) => _catalogConnector.IsWinGetPackage(package);

    private async Task<T> DoWithRecovery<T>(Func<Task<T>> actionFunc)
    {
        const int maxAttempts = 3;
        const int delayMs = 1_000;

        // Run action in a background thread to avoid blocking the UI thread
        // Async methods are blocking in WinGet: https://github.com/microsoft/winget-cli/issues/3205
        return await Task.Run(async () =>
        {
            var attempt = 0;
            while (++attempt <= maxAttempts)
            {
                try
                {
                    return await actionFunc();
                }
                catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
                {
                    if (attempt < maxAttempts)
                    {
                        // Retry with exponential backoff
                        var backoffMs = delayMs * (int)Math.Pow(2, attempt);
                        Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to operate on out-of-proc object with error code: 0x{e.HResult:x}. Attempting to recover in: {backoffMs} ms");
                        await Task.Delay(TimeSpan.FromMilliseconds(backoffMs));
                        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Attempting to recover windows package manager at attempt number: {attempt}");
                        await InitializeAsync();
                    }
                }
            }

            Log.Logger?.ReportError(Log.Component.AppManagement, $"Unable to recover windows package manager after {maxAttempts} attempts");
            throw new WindowsPackageManagerRecoveryException();
        });
    }

    /// <summary>
    /// Check if the package requires elevation
    /// </summary>
    /// <returns>True if the package requires elevation</returns>
    private bool RequiresElevation(CatalogPackage package)
    {
        var packageId = package.Id;
        try
        {
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Getting applicable installer info for package {packageId}");
            var installOptions = _wingetFactory.CreateInstallOptions();
            installOptions.PackageInstallScope = PackageInstallScope.Any;
            var applicableInstaller = package.DefaultInstallVersion.GetApplicableInstaller(installOptions);
            if (applicableInstaller != null)
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Elevation requirement = {applicableInstaller.ElevationRequirement} for package {packageId}");
                return applicableInstaller.ElevationRequirement == ElevationRequirement.ElevationRequired || applicableInstaller.ElevationRequirement == ElevationRequirement.ElevatesSelf;
            }

            Log.Logger?.ReportWarn(Log.Component.AppManagement, $"No applicable installer info found for package {packageId}; defaulting to not requiring elevation");
            return false;
        }
        catch
        {
            Log.Logger?.ReportWarn(Log.Component.AppManagement, $"Failed to get elevation requirement for package {packageId}; defaulting to not requiring elevation");
            return false;
        }
    }

    /// <summary>
    /// Create an in-proc WinGet package from an out-of-proc COM catalog package object
    /// </summary>
    /// <param name="package">COM catalog package</param>
    /// <returns>WinGet package</returns>
    private IWinGetPackage CreateWinGetPackage(CatalogPackage package) => new WinGetPackage(package, RequiresElevation(package));
}
