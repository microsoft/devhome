// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using DevHome.Common.Exceptions;
using DevHome.Common.Services;
using DevHome.Services;
using DevHome.SetupFlow.Common.Extensions;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Models;
using Microsoft.Management.Deployment;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Windows package manager class is an entry point for using the WinGet COM API.
/// </summary>
public class WindowsPackageManager : IWindowsPackageManager
{
    public const int AppInstallerErrorFacility = 0xA15;
    public const string AppInstallerProductId = "9NBLGGH4NNS1";
    public const string AppInstallerPackageFamilyName = "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe";
    public const string Scheme = "x-ms-winget";

    private readonly WindowsPackageManagerFactory _wingetFactory;
    private readonly IAppInstallManagerService _appInstallManagerService;
    private readonly IPackageDeploymentService _packageDeploymentService;

    // Custom composite catalogs
    private readonly Lazy<WinGetCompositeCatalog> _allCatalogs;
    private readonly Lazy<WinGetCompositeCatalog> _wingetCatalog;

    // Predefined catalog ids
    private readonly Lazy<string> _wingetCatalogId;
    private readonly Lazy<string> _msStoreCatalogId;

    public WindowsPackageManager(
        WindowsPackageManagerFactory wingetFactory,
        IAppInstallManagerService appInstallManagerService,
        IPackageDeploymentService packageDeploymentService)
    {
        _wingetFactory = wingetFactory;
        _appInstallManagerService = appInstallManagerService;
        _packageDeploymentService = packageDeploymentService;

        // Lazy-initialize custom composite catalogs
        _allCatalogs = new (CreateAllCatalogs);
        _wingetCatalog = new (CreateWinGetCatalog);

        // Lazy-initialize predefined catalog ids
        _wingetCatalogId = new (() => GetPredefinedCatalogId(PredefinedPackageCatalog.OpenWindowsCatalog));
        _msStoreCatalogId = new (() => GetPredefinedCatalogId(PredefinedPackageCatalog.MicrosoftStore));
    }

    public string WinGetCatalogId => _wingetCatalogId.Value;

    public string MsStoreId => _msStoreCatalogId.Value;

    public IWinGetCatalog AllCatalogs => _allCatalogs.Value;

    public IWinGetCatalog WinGetCatalog => _wingetCatalog.Value;

    public async Task ConnectToAllCatalogsAsync(bool force)
    {
        Log.Logger?.ReportInfo(Log.Component.AppManagement, "Connecting to all catalogs");

        // Connect predefined winget catalog to enable loading
        // package with a known source (e.g. for restoring packages)
        var wingetConnect = Task.Run(async () =>
        {
            try
            {
                await WinGetCatalog.ConnectAsync(force);
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to connect to {WinGetCatalog} when connecting to all catalogs", e);
            }
        });

        // Connect composite catalog for all local and remote catalogs to
        // enable searching for packages from any source
        var allCatalogsConnect = Task.Run(async () =>
        {
            try
            {
                await AllCatalogs.ConnectAsync(force);
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to connect to {AllCatalogs} (search) when connecting to all catalogs", e);
            }
        });

        await Task.WhenAll(wingetConnect, allCatalogsConnect);

        Log.Logger?.ReportInfo(Log.Component.AppManagement, "Connecting to all catalogs completed");
    }

    public async Task<InstallPackageResult> InstallPackageAsync(WinGetPackage package)
    {
        var packageManager = _wingetFactory.CreatePackageManager();
        var options = _wingetFactory.CreateInstallOptions();
        options.PackageInstallMode = PackageInstallMode.Silent;

        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Starting package install for {package.Id}");
        var installResult = await packageManager.InstallPackageAsync(package.CatalogPackage, options).AsTask();
        var extendedErrorCode = installResult.ExtendedErrorCode?.HResult ?? HRESULT.S_OK;

        // Contract version 4
        var installErrorCode = installResult.GetValueOrDefault(res => res.InstallerErrorCode, HRESULT.S_OK);

        Log.Logger?.ReportInfo(
            Log.Component.AppManagement,
            $"Install result: Status={installResult.Status}, InstallerErrorCode={installErrorCode}, ExtendedErrorCode={extendedErrorCode}, RebootRequired={installResult.RebootRequired}");

        if (installResult.Status != InstallResultStatus.Ok)
        {
            throw new InstallPackageException(installResult.Status, extendedErrorCode, installErrorCode);
        }

        return new ()
        {
            ExtendedErrorCode = extendedErrorCode,
            RebootRequired = installResult.RebootRequired,
        };
    }

    public async Task<bool> IsAppInstallerUpdateAvailableAsync()
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

    public async Task<bool> StartAppInstallerUpdateAsync()
    {
        try
        {
            Log.Logger?.ReportInfo(Log.Component.AppManagement, "Starting AppInstaller update ...");
            var updateStarted = await _appInstallManagerService.StartAppUpdateAsync(AppInstallerProductId);
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Start AppInstaller update = {updateStarted}");
            return updateStarted;
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, "Failed to start AppInstaller update", e);
            return false;
        }
    }

    public async Task<bool> RegisterAppInstallerAsync()
    {
        try
        {
            Log.Logger?.ReportInfo(Log.Component.AppManagement, "Starting AppInstaller registration ...");
            await _packageDeploymentService.RegisterPackageForCurrentUserAsync(AppInstallerPackageFamilyName);
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"AppInstaller registered succcessfully");
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

    public Uri CreatePackageUri(IWinGetPackage package)
    {
        if (package.CatalogId == WinGetCatalogId)
        {
            return new Uri($"{Scheme}://winget/{package.Id}");
        }

        throw new NotSupportedException($"Creating a package uri from catalog '{package.CatalogName}' is not supported");
    }

    /// <summary>
    /// Gets the id of the provided predefined catalog
    /// </summary>
    /// <param name="catalog">Predefined catalog</param>
    /// <returns>Catalog id</returns>
    private string GetPredefinedCatalogId(PredefinedPackageCatalog catalog)
    {
        var packageManager = _wingetFactory.CreatePackageManager();
        var packageCatalog = packageManager.GetPredefinedPackageCatalog(catalog);
        return packageCatalog.Info.Id;
    }

    /// <summary>
    /// Create a composite catalog that can be used for finding packages in all
    /// remote and local packages
    /// </summary>
    /// <returns>Catalog composed of all remote and local catalogs</returns>
    private WinGetCompositeCatalog CreateAllCatalogs()
    {
        var compositeCatalog = new WinGetCompositeCatalog(_wingetFactory);
        compositeCatalog.CompositeSearchBehavior = CompositeSearchBehavior.RemotePackagesFromAllCatalogs;
        var packageManager = _wingetFactory.CreatePackageManager();
        var catalogs = packageManager.GetPackageCatalogs();

        // Cannot use foreach or LINQ for out-of-process IVector
        // Bug: https://github.com/microsoft/CsWinRT/issues/1205
        for (var i = 0; i < catalogs.Count; ++i)
        {
            compositeCatalog.AddPackageCatalog(catalogs[i]);
        }

        return compositeCatalog;
    }

    /// <summary>
    /// Create a composite catalog that can be used for finding packages in
    /// winget and local catalogs
    /// </summary>
    /// <returns>Catalog composed of winget and local catalogs</returns>
    private WinGetCompositeCatalog CreateWinGetCatalog()
    {
        return CreatePredefinedCatalog(PredefinedPackageCatalog.OpenWindowsCatalog);
    }

    /// <summary>
    /// Create a composite catalog that can be used for finding packages in
    /// a predefined and local catalogs
    /// </summary>
    /// <param name="predefinedPackageCatalog">Predefined package catalog</param>
    /// <returns>Catalog composed of the provided and local catalogs</returns>
    private WinGetCompositeCatalog CreatePredefinedCatalog(PredefinedPackageCatalog predefinedPackageCatalog)
    {
        var compositeCatalog = new WinGetCompositeCatalog(_wingetFactory);
        compositeCatalog.CompositeSearchBehavior = CompositeSearchBehavior.RemotePackagesFromAllCatalogs;
        var packageManager = _wingetFactory.CreatePackageManager();
        var catalog = packageManager.GetPredefinedPackageCatalog(predefinedPackageCatalog);
        compositeCatalog.AddPackageCatalog(catalog);
        return compositeCatalog;
    }

    /// <summary>
    /// Check if WindowsPackageManager COM Server is available by creating a
    /// dummy out-of-proc object
    /// </summary>
    /// <returns>True if server is available, false otherwise.</returns>
    public async Task<bool> IsCOMServerAvailableAsync()
    {
        try
        {
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
}
