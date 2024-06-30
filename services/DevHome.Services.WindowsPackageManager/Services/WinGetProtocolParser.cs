// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DevHome.Services.WindowsPackageManager.Contracts;
using DevHome.Services.WindowsPackageManager.Models;

namespace DevHome.Services.WindowsPackageManager.Services;

/// <summary>
/// Winget protocol parser
/// Protocol scheme: x-ms-winget://[catalog]/[packageId]
/// </summary>
internal sealed class WinGetProtocolParser : IWinGetProtocolParser
{
    private readonly IWinGetCatalogConnector _catalogConnector;

    public WinGetProtocolParser(IWinGetCatalogConnector catalogConnector)
    {
        _catalogConnector = catalogConnector;
    }

    /// <summary>
    /// Reserved URI name for the WinGet catalog
    /// </summary>
    private const string ReservedWingetCatalogURIName = "winget";

    /// <summary>
    /// Reserved URI name for the Microsoft Store catalog
    /// </summary>
    private const string ReservedMsStoreCatalogURIName = "msstore";

    /// <inheritdoc/>
    public async Task<WinGetCatalog> ResolveCatalogAsync(WinGetPackageUri packageUri)
    {
        var catalogName = packageUri.CatalogName;

        // 'winget' catalog
        if (catalogName == ReservedWingetCatalogURIName)
        {
            return await _catalogConnector.GetPredefinedWingetCatalogAsync();
        }

        // 'msstore' catalog
        if (catalogName == ReservedMsStoreCatalogURIName)
        {
            return await _catalogConnector.GetPredefinedMsStoreCatalogAsync();
        }

        // custom catalog
        return await _catalogConnector.GetPackageCatalogByNameAsync(catalogName);
    }

    /// <inheritdoc/>
    public WinGetPackageUri CreateWinGetCatalogPackageUri(string packageId) => new(ReservedWingetCatalogURIName, packageId);

    /// <inheritdoc/>
    public WinGetPackageUri CreateMsStoreCatalogPackageUri(string packageId) => new(ReservedMsStoreCatalogURIName, packageId);

    /// <inheritdoc/>
    public WinGetPackageUri CreateCustomCatalogPackageUri(string packageId, string catalogName) => new(catalogName, packageId);

    /// <inheritdoc/>
    public WinGetPackageUri CreatePackageUri(string packageId, WinGetCatalog catalog)
    {
        return catalog.Type switch
        {
            WinGetCatalog.CatalogType.PredefinedWinget => CreateWinGetCatalogPackageUri(packageId),
            WinGetCatalog.CatalogType.PredefinedMsStore => CreateMsStoreCatalogPackageUri(packageId),
            WinGetCatalog.CatalogType.CustomSearch => throw new ArgumentException("Custom search catalog is an invalid input argument"),
            _ => CreateCustomCatalogPackageUri(packageId, catalog.UnknownCatalogName),
        };
    }

    /// <inheritdoc/>
    public WinGetPackageUri CreatePackageUri(IWinGetPackage package) => CreateCustomCatalogPackageUri(package.Id, package.CatalogName);
}
