// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services.WinGet;
using Microsoft.Management.Deployment;

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Windows package manager class is an entry point for using the WinGet COM API.
/// </summary>
public class WindowsPackageManager : IWindowsPackageManager
{
    // WinGet services
    private readonly IWinGetCatalogConnector _catalogConnector;
    private readonly IWinGetPackageFinder _packageFinder;
    private readonly IWinGetPackageInstaller _packageInstaller;
    private readonly IWinGetProtocolParser _protocolParser;
    private readonly IWinGetDeployment _deployment;
    private readonly IWinGetRecovery _recovery;

    public static string AppInstallerProductId => WinGetDeployment.AppInstallerProductId;

    public static int AppInstallerErrorFacility => WinGetDeployment.AppInstallerErrorFacility;

    public WindowsPackageManager(
        IWinGetCatalogConnector catalogConnector,
        IWinGetPackageFinder packageFinder,
        IWinGetPackageInstaller packageInstaller,
        IWinGetProtocolParser protocolParser,
        IWinGetDeployment deployment,
        IWinGetRecovery recovery)
    {
        _catalogConnector = catalogConnector;
        _packageFinder = packageFinder;
        _packageInstaller = packageInstaller;
        _protocolParser = protocolParser;
        _deployment = deployment;
        _recovery = recovery;
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
        return await _recovery.DoWithRecovery(async () =>
        {
            var catalog = await _catalogConnector.GetPackageCatalogAsync(package);
            return await _packageInstaller.InstallPackageAsync(catalog, package.Id);
        });
    }

    /// <inheritdoc/>
    public async Task<IList<IWinGetPackage>> GetPackagesAsync(ISet<Uri> packageUriSet)
    {
        // Get packages grouped by catalog
        var getPackagesTasks = new List<Task<List<IWinGetPackage>>>();
        var parsedUrisByCatalog = GroupParsedUrisByCatalog(packageUriSet);
        foreach (var parsedUrisGroup in parsedUrisByCatalog)
        {
            if (parsedUrisGroup.Any())
            {
                // Get packages from each catalog concurrently
                getPackagesTasks.Add(_recovery.DoWithRecovery(async () =>
                {
                    // All parsed uris in the group have the same catalog, resolve catalog from the first entry
                    var firstParsedUri = parsedUrisGroup.First();
                    var packageIds = parsedUrisGroup.Select(p => p.packageId).ToHashSet();
                    Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Getting packages from parsed uri catalog name: {firstParsedUri.catalogUriName}");

                    // Get packages from the catalog
                    var catalog = await _protocolParser.ResolveCatalogAsync(firstParsedUri);
                    var packages = await _packageFinder.GetPackagesAsync(catalog, packageIds);
                    return packages.Select(p => CreateWinGetPackage(p)).ToList();
                }));
            }
        }

        // Wait for all packages to be retrieved
        await Task.WhenAll(getPackagesTasks);
        return getPackagesTasks.SelectMany(p => p.Result).ToList();
    }

    /// <inheritdoc/>
    public async Task<IList<IWinGetPackage>> SearchAsync(string query, uint limit = 0)
    {
        return await _recovery.DoWithRecovery(async () =>
        {
            var searchCatalog = await _catalogConnector.GetCustomSearchCatalogAsync();
            var results = await _packageFinder.SearchAsync(searchCatalog, query, limit);
            return results.Select(p => CreateWinGetPackage(p)).ToList();
        });
    }

    /// <inheritdoc/>
    public async Task<bool> IsUpdateAvailableAsync() => await _deployment.IsUpdateAvailableAsync();

    /// <inheritdoc/>
    public async Task<bool> RegisterAppInstallerAsync() => await _deployment.RegisterAppInstallerAsync();

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync() => await _deployment.IsAvailableAsync();

    /// <inheritdoc/>
    public bool IsMsStorePackage(IWinGetPackage package) => _catalogConnector.IsMsStorePackage(package);

    /// <inheritdoc/>
    public bool IsWinGetPackage(IWinGetPackage package) => _catalogConnector.IsWinGetPackage(package);

    /// <summary>
    /// Create an in-proc WinGet package from an out-of-proc COM catalog package object
    /// </summary>
    /// <param name="package">COM catalog package</param>
    /// <returns>WinGet package</returns>
    private IWinGetPackage CreateWinGetPackage(CatalogPackage package) => new WinGetPackage(package, _packageInstaller.IsElevationRequired(package));

    /// <summary>
    /// Group packages by their catalogs
    /// </summary>
    /// <param name="packageUriSet">Set of package uris</param>
    /// <returns>Dictionary of package ids by catalog</returns>
    private List<List<WinGetProtocolParserResult>> GroupParsedUrisByCatalog(ISet<Uri> packageUriSet)
    {
        var parsedUris = new List<WinGetProtocolParserResult>();

        // 1. Parse all package uris and log invalid ones
        foreach (var packageUri in packageUriSet)
        {
            var uriInfo = _protocolParser.ParsePackageUri(packageUri);
            if (uriInfo != null)
            {
                parsedUris.Add(uriInfo);
            }
            else
            {
                Log.Logger?.ReportWarn(Log.Component.AppManagement, $"Failed to get uri details from '{packageUri}'");
            }
        }

        // 2. Group package ids by catalog
        return parsedUris.GroupBy(p => p.catalogUriName).Select(p => p.ToList()).ToList();
    }
}
