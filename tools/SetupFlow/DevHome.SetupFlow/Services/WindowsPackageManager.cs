// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DevHome.Common.Exceptions;
using DevHome.Common.Services;
using DevHome.Services;
using DevHome.SetupFlow.Common.Extensions;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Extensions;
using DevHome.SetupFlow.Models;
using Microsoft.Management.Deployment;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Windows package manager class is an entry point for using the WinGet COM API.
/// </summary>
public class WindowsPackageManager : IWindowsPackageManager, IDisposable
{
    // App installer constants
    public const int AppInstallerErrorFacility = 0xA15;
    public const string AppInstallerProductId = "9NBLGGH4NNS1";
    public const string AppInstallerPackageFamilyName = "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe";

    // COM error codes
    public const int RpcServerUnavailable = unchecked((int)0x800706BA);
    public const int RpcCallFailed = unchecked((int)0x800706BE);

    // Package manager URI constants:
    // - x-ms-winget: is a custom scheme for WinGet package manager
    // - winget: is a reserved URI name for the winget catalog
    public const string Scheme = "x-ms-winget";
    public const string WingetCatalogURIName = "winget";
    public const string MsStoreCatalogURIName = "msstore";

    private readonly WindowsPackageManagerFactory _wingetFactory;
    private readonly IAppInstallManagerService _appInstallManagerService;
    private readonly IPackageDeploymentService _packageDeploymentService;
    private readonly Dictionary<string, Microsoft.Management.Deployment.PackageCatalog> _customCatalogs = new ();
    private readonly SemaphoreSlim _initLock = new (1, 1);
    private readonly SemaphoreSlim _catalogLock = new (1, 1);

    private bool _disposedValue;
    private Microsoft.Management.Deployment.PackageCatalog _searchCatalog;
    private Microsoft.Management.Deployment.PackageCatalog _wingetCatalog;
    private Microsoft.Management.Deployment.PackageCatalog _msStoreCatalog;
    private string _wingetCatalogId;
    private string _msStoreId;

    public WindowsPackageManager(
        WindowsPackageManagerFactory wingetFactory,
        IAppInstallManagerService appInstallManagerService,
        IPackageDeploymentService packageDeploymentService)
    {
        _wingetFactory = wingetFactory;
        _appInstallManagerService = appInstallManagerService;
        _packageDeploymentService = packageDeploymentService;
    }

    public bool IsInitialized { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            await _initLock.WaitAsync();
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Begin initializing WPM");

            // Skip if already initialized
            if (IsInitialized)
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Initialization of WPM was skipped because it is already initialized");
                return;
            }

            await CreateAndConnectCatalogsAsync();

            // Extract catalog ids for predefined catalogs
            _wingetCatalogId ??= GetPredefinedCatalogId(PredefinedPackageCatalog.OpenWindowsCatalog);
            _msStoreId ??= GetPredefinedCatalogId(PredefinedPackageCatalog.MicrosoftStore);

            // Mark as initialized
            IsInitialized = true;
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Initializing WPM completed");
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task CreateAndConnectCatalogsAsync()
    {
        // Create and connect to predefined catalogs concurrently
        var searchCatalog = CreateAndConnectSearchCatalogAsync();
        var wingetCatalog = CreateAndConnectWinGetCatalogAsync();
        var msStoreCatalog = CreateAndConnectMsStoreCatalogAsync();
        await Task.WhenAll(searchCatalog, wingetCatalog, msStoreCatalog);
        _searchCatalog = searchCatalog.Result;
        _wingetCatalog = wingetCatalog.Result;
        _msStoreCatalog = msStoreCatalog.Result;

        // Clear custom catalogs
        await _catalogLock.WaitAsync();
        try
        {
            _customCatalogs.Clear();
        }
        finally
        {
            _catalogLock.Release();
        }
    }

    private async Task<Microsoft.Management.Deployment.PackageCatalog> GetCustomPackageCatalogAsync(string catalogName)
    {
        // Get custom catalog from cache or connect to it then cache it
        await _catalogLock.WaitAsync();
        try
        {
            if (_customCatalogs.TryGetValue(catalogName, out var catalog))
            {
                return catalog;
            }

            var packageManager = _wingetFactory.CreatePackageManager();
            var customCatalog = packageManager.GetPackageCatalogByName(catalogName);
            var result = await customCatalog.ConnectAsync();
            if (result.Status != ConnectResultStatus.Ok)
            {
                throw new InvalidOperationException($"Failed to connect to catalog {catalogName} with status {result.Status}");
            }

            _customCatalogs[catalogName] = result.PackageCatalog;
            return result.PackageCatalog;
        }
        finally
        {
            _catalogLock.Release();
        }
    }

    private async Task<Microsoft.Management.Deployment.PackageCatalog> GetPackageCatalogAsync(IWinGetPackage package)
    {
        if (package.CatalogId == _wingetCatalogId)
        {
            return _wingetCatalog;
        }

        if (package.CatalogId == _msStoreId)
        {
            return _wingetCatalog;
        }

        return await GetCustomPackageCatalogAsync(package.CatalogName);
    }

    public async Task<InstallPackageResult> InstallPackageAsync(IWinGetPackage package, Guid activityId)
    {
        return await DoWithRecovery(async () =>
        {
            // 1. Find package
            var catalog = await GetPackageCatalogAsync(package);
            var findOptions = _wingetFactory.CreateFindPackagesOptions();
            var filter = _wingetFactory.CreatePackageMatchFilter(PackageMatchField.Id, PackageFieldMatchOption.Equals, package.Id);
            findOptions.Filters.Add(filter);
            var matches = await catalog.FindPackagesAsync(findOptions);
            if (matches.Matches.Count == 0)
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Install aborted for package {package.Id} because it was not found in catalog {package.CatalogName}");
                throw new FindPackagesException(FindPackagesResultStatus.CatalogError);
            }

            // 2. Install package
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Starting package install for {package.Id}");
            var installOptions = _wingetFactory.CreateInstallOptions();
            installOptions.PackageInstallMode = PackageInstallMode.Silent;
            var packageManager = _wingetFactory.CreatePackageManager();
            var installResult = await packageManager.InstallPackageAsync(matches.Matches[0].CatalogPackage, installOptions).AsTask();
            var extendedErrorCode = installResult.ExtendedErrorCode?.HResult ?? HRESULT.S_OK;
            var installErrorCode = installResult.GetValueOrDefault(res => res.InstallerErrorCode, HRESULT.S_OK); // WPM API V4

            // 3. Report install result
            Log.Logger?.ReportInfo(
                Log.Component.AppManagement,
                $"Install result: Status={installResult.Status}, InstallerErrorCode={installErrorCode}, ExtendedErrorCode={extendedErrorCode}, RebootRequired={installResult.RebootRequired}");
            if (installResult.Status != InstallResultStatus.Ok)
            {
                throw new InstallPackageException(installResult.Status, extendedErrorCode, installErrorCode);
            }

            return new InstallPackageResult()
            {
                ExtendedErrorCode = extendedErrorCode,
                RebootRequired = installResult.RebootRequired,
            };
        });
    }

    public async Task<IList<IWinGetPackage>> GetPackagesAsync(ISet<Uri> packageUriSet)
    {
        return await DoWithRecovery(async () =>
        {
            Dictionary<Microsoft.Management.Deployment.PackageCatalog, HashSet<string>> packageIdsByCatalog = new ();
            foreach (var packageUri in packageUriSet)
            {
                var packageInfo = await GetPackageIdAndCatalogAsync(packageUri);
                if (packageInfo.HasValue)
                {
                    var packageId = packageInfo.Value.Item1;
                    var catalog = packageInfo.Value.Item2;

                    if (!packageIdsByCatalog.ContainsKey(catalog))
                    {
                        packageIdsByCatalog[catalog] = new HashSet<string>();
                    }

                    packageIdsByCatalog[catalog].Add(packageId);
                }
                else
                {
                    Log.Logger?.ReportWarn(Log.Component.AppManagement, $"Failed to get package id from uri '{packageUri}'");
                }
            }

            var result = new List<IWinGetPackage>();
            foreach (var catalog in packageIdsByCatalog.Keys)
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Getting packages from catalog {catalog.Info.Name}");
                result.AddRange(await GetPackagesAsync(catalog, packageIdsByCatalog[catalog]));
            }

            return result;
        });
    }

    public async Task<IList<IWinGetPackage>> SearchAsync(string query, uint limit = 0)
    {
        return await DoWithRecovery(async () =>
        {
            try
            {
                // Use default filter criteria for searching
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Searching for '{query}'. Result limit: {limit}");
                var options = _wingetFactory.CreateFindPackagesOptions();
                var filter = _wingetFactory.CreatePackageMatchFilter(PackageMatchField.CatalogDefault, PackageFieldMatchOption.ContainsCaseInsensitive, query);
                options.Selectors.Add(filter);
                options.ResultLimit = limit;
                return await FindPackagesAsync(_searchCatalog, options);
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.AppManagement, $"Error searching for packages.", e);
                throw;
            }
        });
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

    /// <summary>
    /// Check if WindowsPackageManager COM Server is available by creating a
    /// dummy out-of-proc object
    /// </summary>
    /// <returns>True if server is available, false otherwise.</returns>
    public async Task<bool> IsCOMServerAvailableAsync()
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

    public bool IsMsStorePackage(IWinGetPackage package) => package.CatalogId == _msStoreId;

    private async Task<IList<IWinGetPackage>> GetPackagesAsync(Microsoft.Management.Deployment.PackageCatalog catalog, ISet<string> packageIdSet)
    {
        try
        {
            // Skip search if set is empty
            if (!packageIdSet.Any())
            {
                Log.Logger?.ReportWarn(Log.Component.AppManagement, $"{nameof(GetPackagesAsync)} received an empty set of package id. Skipping search.");
                return new List<IWinGetPackage>();
            }

            if (catalog == _wingetCatalog)
            {
                return await GetPackagesSingleQueryAsync(_wingetCatalog, packageIdSet);
            }

            return await GetPackagesMultiQueriesAsync(catalog, packageIdSet);
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Error getting packages.", e);
            throw;
        }
    }

    private async Task<IList<IWinGetPackage>> GetPackagesMultiQueriesAsync(Microsoft.Management.Deployment.PackageCatalog catalog, ISet<string> packageIdSet)
    {
        var result = new List<IWinGetPackage>();
        foreach (var packageId in packageIdSet)
        {
            var options = _wingetFactory.CreateFindPackagesOptions();
            var filter = _wingetFactory.CreatePackageMatchFilter(PackageMatchField.Id, PackageFieldMatchOption.Equals, packageId);
            options.Filters.Add(filter);
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Starting search for package [{packageId}]");
            var matches = await FindPackagesAsync(catalog, options);
            if (matches.Count > 0)
            {
                result.Add(matches[0]);
            }
            else
            {
                Log.Logger?.ReportWarn(Log.Component.AppManagement, $"Package [{packageId}] not found. Skipping ...");
            }
        }

        return result;
    }

    private async Task<IList<IWinGetPackage>> GetPackagesSingleQueryAsync(Microsoft.Management.Deployment.PackageCatalog catalog, ISet<string> packageIdSet)
    {
        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Getting package set from catalog {catalog.Info.Name}");
        var options = _wingetFactory.CreateFindPackagesOptions();
        foreach (var packageId in packageIdSet)
        {
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Adding package [{packageId}] to query");
            var filter = _wingetFactory.CreatePackageMatchFilter(PackageMatchField.Id, PackageFieldMatchOption.Equals, packageId);
            options.Selectors.Add(filter);
        }

        Log.Logger?.ReportInfo(Log.Component.AppManagement, "Starting search for packages");
        return await FindPackagesAsync(catalog, options);
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
    /// TryGet the package id and catalog from a package uri
    /// </summary>
    /// <param name="packageUri">Input package uri</param>
    /// <returns>True if the package uri is valid and a package id was identified, false otherwise.</returns>
    private async Task<(string, Microsoft.Management.Deployment.PackageCatalog)?> GetPackageIdAndCatalogAsync(Uri packageUri)
    {
        if (packageUri.Scheme == Scheme && packageUri.Segments.Length == 2)
        {
            var packageId = packageUri.Segments[1];
            if (packageUri.Host == WingetCatalogURIName)
            {
                return (packageId, _wingetCatalog);
            }

            if (packageUri.Host == MsStoreCatalogURIName)
            {
                return (packageId, _msStoreCatalog);
            }

            return (packageId, await GetCustomPackageCatalogAsync(packageUri.Host));
        }

        return null;
    }

    private async Task<Microsoft.Management.Deployment.PackageCatalog> CreateAndConnectSearchCatalogAsync()
    {
        var packageManager = _wingetFactory.CreatePackageManager();
        var catalogs = packageManager.GetPackageCatalogs();
        return await CreateAndConnectCatalogAsync(catalogs);
    }

    private async Task<Microsoft.Management.Deployment.PackageCatalog> CreateAndConnectWinGetCatalogAsync()
    {
        var packageManager = _wingetFactory.CreatePackageManager();
        var catalog = packageManager.GetPredefinedPackageCatalog(PredefinedPackageCatalog.OpenWindowsCatalog);
        return await CreateAndConnectCatalogAsync(new List<PackageCatalogReference>() { catalog });
    }

    private async Task<Microsoft.Management.Deployment.PackageCatalog> CreateAndConnectMsStoreCatalogAsync()
    {
        var packageManager = _wingetFactory.CreatePackageManager();
        var catalog = packageManager.GetPredefinedPackageCatalog(PredefinedPackageCatalog.MicrosoftStore);
        return await CreateAndConnectCatalogAsync(new List<PackageCatalogReference>() { catalog });
    }

    private async Task<Microsoft.Management.Deployment.PackageCatalog> CreateAndConnectCatalogAsync(IReadOnlyList<PackageCatalogReference> catalogReferences)
    {
        // Search in all catalogs including the local catalog which allows detecting if a package is installed
        var disconnectedCatalog = _wingetFactory.CreateCompositePackageCatalog(CompositeSearchBehavior.RemotePackagesFromAllCatalogs, catalogReferences);
        var connectResult = await disconnectedCatalog.ConnectAsync();
        if (connectResult.Status == ConnectResultStatus.Ok)
        {
            return connectResult.PackageCatalog;
        }

        Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to connect to catalog with status {connectResult.Status}");
        return null;
    }

    /// <summary>
    /// Core method for finding packages based on the provided options
    /// </summary>
    /// <param name="options">Find packages options</param>
    /// <returns>List of winget package matches</returns>
    /// <exception cref="InvalidOperationException">Exception thrown if the catalog is not connected before attempting to find packages</exception>
    /// <exception cref="FindPackagesException">Exception thrown if the find packages operation failed</exception>
    private async Task<IList<IWinGetPackage>> FindPackagesAsync(Microsoft.Management.Deployment.PackageCatalog catalog, FindPackagesOptions options)
    {
        Log.Logger?.ReportInfo(Log.Component.AppManagement, "Performing search");
        var result = new List<IWinGetPackage>();
        var findResult = await catalog.FindPackagesAsync(options);
        if (findResult.Status != FindPackagesResultStatus.Ok)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to find packages with status {findResult.Status}");
            throw new FindPackagesException(findResult.Status);
        }

        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Found {findResult.Matches} results");

        // Cannot use foreach or LINQ for out-of-process IVector
        // Bug: https://github.com/microsoft/CsWinRT/issues/1205
        for (var i = 0; i < findResult.Matches.Count; ++i)
        {
            var catalogPackage = findResult.Matches[i].CatalogPackage;
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Found [{catalogPackage.Id}]");
            var installOptions = _wingetFactory.CreateInstallOptions();
            installOptions.PackageInstallScope = PackageInstallScope.Any;
            result.Add(new WinGetPackage(catalogPackage, installOptions));
        }

        return result;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _initLock.Dispose();
                _catalogLock.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private async Task<T> DoWithRecovery<T>(Func<Task<T>> actionFunc)
    {
        const int maxRetries = 3;
        const int delayMs = 1_000;

        // Run action in a background thread to avoid blocking the UI thread
        // Async methods are blocking in WinGet: https://github.com/microsoft/winget-cli/issues/3205
        return await Task.Run(async () =>
        {
            var retry = 0;
            while (++retry <= maxRetries)
            {
                try
                {
                    await InitializeAsync();
                    return await actionFunc();
                }
                catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
                {
                    IsInitialized = false;

                    if (retry < maxRetries)
                    {
                        // Retry with exponential backoff
                        var backoffMs = delayMs * (int)Math.Pow(2, retry);
                        Log.Logger?.ReportError(
                            Log.Component.AppManagement,
                            $"Failed to operate on out-of-proc object with error code: 0x{e.HResult:x}. Attempting to recover in: {backoffMs} ms");
                        await Task.Delay(TimeSpan.FromMilliseconds(backoffMs));
                    }
                }
            }

            Log.Logger?.ReportError(Log.Component.AppManagement, $"Unable to recover windows package manager after {maxRetries} retries");
            throw new WindowsPackageManagerRecoveryException();
        });
    }
}
