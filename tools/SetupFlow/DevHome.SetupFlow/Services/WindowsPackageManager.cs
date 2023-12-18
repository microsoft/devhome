// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Helpers;
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
    // COM error codes
    public const int RpcServerUnavailable = unchecked((int)0x800706BA);
    public const int RpcCallFailed = unchecked((int)0x800706BE);

    private readonly IWinGetCatalogConnector _catalogConnector;
    private readonly IWinGetPackageFinder _packageFinder;
    private readonly IWinGetPackageInstaller _packageInstaller;
    private readonly IWinGetProtocolParser _protocolParser;
    private readonly IWinGetDeployment _deployment;

    public static string AppInstallerProductId => WinGetDeployment.AppInstallerProductId;

    public static int AppInstallerErrorFacility => WinGetDeployment.AppInstallerErrorFacility;

    public WindowsPackageManager(
        IWinGetCatalogConnector catalogConnector,
        IWinGetPackageFinder packageFinder,
        IWinGetPackageInstaller packageInstaller,
        IWinGetProtocolParser protocolParser,
        IWinGetDeployment deployment)
    {
        _catalogConnector = catalogConnector;
        _packageFinder = packageFinder;
        _packageInstaller = packageInstaller;
        _protocolParser = protocolParser;
        _deployment = deployment;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        // Run action in a background thread to avoid blocking the UI thread
        // Async methods are blocking in WinGet: https://github.com/microsoft/winget-cli/issues/3205
        await Task.Run(async () => await _catalogConnector.CreateAndConnectCatalogsAsync());
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
            var packageIdsByCatalog = await GroupPackageIdsByCatalogAsync(packageUriSet);
            var result = new List<IWinGetPackage>();
            foreach (var catalog in packageIdsByCatalog.Keys)
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Getting packages from catalog: {catalog.GetDescriptiveName()}");
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
        return await _deployment.IsUpdateAvailableAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> RegisterAppInstallerAsync()
    {
        return await _deployment.RegisterAppInstallerAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync()
    {
        return await _deployment.IsAvailableAsync();
    }

    /// <inheritdoc/>
    public bool IsMsStorePackage(IWinGetPackage package) => _catalogConnector.IsMsStorePackage(package);

    /// <inheritdoc/>
    public bool IsWinGetPackage(IWinGetPackage package) => _catalogConnector.IsWinGetPackage(package);

    /// <summary>
    /// Group packages by their catalogs
    /// </summary>
    /// <param name="packageUriSet">Set of package uris</param>
    /// <returns>Dictionary of package ids by catalog</returns>
    private async Task<Dictionary<WinGetCatalog, HashSet<string>>> GroupPackageIdsByCatalogAsync(ISet<Uri> packageUriSet)
    {
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

        return packageIdsByCatalog;
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
    /// Create an in-proc WinGet package from an out-of-proc COM catalog package object
    /// </summary>
    /// <param name="package">COM catalog package</param>
    /// <returns>WinGet package</returns>
    private IWinGetPackage CreateWinGetPackage(CatalogPackage package) => new WinGetPackage(package, _packageInstaller.IsElevationRequired(package));
}
