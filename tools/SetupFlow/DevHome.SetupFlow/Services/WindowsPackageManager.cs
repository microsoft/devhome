// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
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
using WPMPackageCatalog = Microsoft.Management.Deployment.PackageCatalog;

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

    private readonly WindowsPackageManagerFactory _wingetFactory;
    private readonly IAppInstallManagerService _appInstallManagerService;
    private readonly IPackageDeploymentService _packageDeploymentService;
    private readonly IWinGetPackageFinder _packageFinder;
    private readonly IWinGetPackageInstaller _packageInstaller;
    private readonly SemaphoreSlim _initLock = new (1, 1);

    private volatile uint _session;

    private bool _disposedValue;

    public WindowsPackageManager(
        WindowsPackageManagerFactory wingetFactory,
        IWinGetPackageFinder packageFinder,
        IWinGetPackageInstaller packageInstaller,
        IAppInstallManagerService appInstallManagerService,
        IPackageDeploymentService packageDeploymentService)
    {
        _wingetFactory = wingetFactory;
        _appInstallManagerService = appInstallManagerService;
        _packageDeploymentService = packageDeploymentService;
        _packageFinder = packageFinder;
        _packageInstaller = packageInstaller;
    }

    public async Task InitializeAsync()
    {
        // Run action in a background thread to avoid blocking the UI thread
        // Async methods are blocking in WinGet: https://github.com/microsoft/winget-cli/issues/3205
        await Task.Run(async () =>
        {
            await _initLock.WaitAsync();

            try
            {
                await InitializeInternalAsync(_session);
            }
            finally
            {
                _initLock.Release();
            }
        });
    }

    public async Task ReconnectCatalogsAsync() => await InitializeAsync();

    public async Task<InstallPackageResult> InstallPackageAsync(IWinGetPackage package, Guid activityId)
    {
        return await DoWithRecovery(async () =>
        {
            return await _packageInstaller.InstallPackageAsync(null, package.Id);
        });
    }

    public async Task<IList<IWinGetPackage>> GetPackagesAsync(ISet<Uri> packageUriSet)
    {
        return await DoWithRecovery(async () =>
        {
            // 1. Group packages by their catalogs
            Dictionary<WPMPackageCatalog, HashSet<string>> packageIdsByCatalog = new ();
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
                    Log.Logger?.ReportWarn(Log.Component.AppManagement, $"Failed to get package details from uri '{packageUri}'");
                }
            }

            // 2. Get packages from each catalog
            var result = new List<IWinGetPackage>();
            foreach (var catalog in packageIdsByCatalog.Keys)
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Getting packages from catalog {catalog.Info.Name}");

                // Get

                result.AddRange();
            }

            return result;
        });
    }

    public async Task<IList<IWinGetPackage>> SearchAsync(string query, uint limit = 0)
    {
        return await DoWithRecovery(async () =>
        {
            // Search
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

    public bool IsMsStorePackage(IWinGetPackage package) => package.CatalogId == _msStoreCatalogId;

    public bool IsWinGetPackage(IWinGetPackage package) => package.CatalogId == _wingetCatalogId;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _initLock.Dispose();
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
                        Log.Logger?.ReportError(
                            Log.Component.AppManagement,
                            $"Failed to operate on out-of-proc object with error code: 0x{e.HResult:x}. Attempting to recover in: {backoffMs} ms");
                        await Task.Delay(TimeSpan.FromMilliseconds(backoffMs));

                        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Attempting to recover windows package manager at attempt number: {attempt}");

                        // Reinitialize the package manager
                        _initLock.Wait();
                        try
                        {
                            await InitializeInternalAsync(_session);
                        }
                        catch
                        {
                            // No-op
                        }
                        finally
                        {
                            _initLock.Release();
                        }
                    }
                }
            }

            Log.Logger?.ReportError(Log.Component.AppManagement, $"Unable to recover windows package manager after {maxAttempts} attempts");
            throw new WindowsPackageManagerRecoveryException();
        });
    }

    /// <summary>
    /// Initialize the package manager
    /// </summary>
    /// <param name="requestSession">Request session id</param>
    /// <remarks>
    /// If the provided session id is older than the current session, then the initialization is skipped.
    /// </remarks>
    private async Task InitializeInternalAsync(long requestSession)
    {
        // If the initialization was requested from an older session, then skip it
        if (requestSession < _session)
        {
            return;
        }

        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Begin initializing WPM");
        await CreateAndConnectCatalogsAsync();

        // New session
        _session++;
        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Initializing WPM completed");
    }
}
