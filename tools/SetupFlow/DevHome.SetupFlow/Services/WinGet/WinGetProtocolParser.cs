// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet;

public record class WinGetProtocolParserResult(string packageId, WinGetCatalog catalog);

/// <summary>
/// Winget protocol parser
/// Protocol scheme: x-ms-winget://[catalog]/[packageId]
/// </summary>
public class WinGetProtocolParser : IWinGetProtocolParser
{
    private readonly IWinGetCatalogConnector _catalogConnector;

    public WinGetProtocolParser(IWinGetCatalogConnector catalogConnector)
    {
        _catalogConnector = catalogConnector;
    }

    /// <summary>
    /// Windows package manager custom protocol scheme
    /// </summary>
    private const string Scheme = "x-ms-winget";

    /// <summary>
    /// Reserved name for the WinGet catalog
    /// </summary>
    private const string WingetCatalogURIName = "winget";

    /// <summary>
    /// Reserved name for the Microsoft Store catalog
    /// </summary>
    private const string MsStoreCatalogURIName = "msstore";

    /// <inheritdoc/>
    public async Task<WinGetProtocolParserResult> ParsePackageUriAsync(Uri packageUri)
    {
        if (packageUri.Scheme == Scheme && packageUri.Segments.Length == 2)
        {
            var packageId = packageUri.Segments[1];
            var catalogName = packageUri.Host;

            // 'winget' catalog
            if (catalogName == WingetCatalogURIName)
            {
                return new (packageId, await _catalogConnector.GetPredefinedWingetCatalogAsync());
            }

            // 'msstore' catalog
            if (catalogName == MsStoreCatalogURIName)
            {
                return new (packageId, await _catalogConnector.GetPredefinedMsStoreCatalogAsync());
            }

            // custom catalog
            return new (packageId, await _catalogConnector.GetPackageCatalogByNameAsync(catalogName));
        }

        return null;
    }

    /// <inheritdoc/>
    public Uri CreateWinGetCatalogPackageUri(string packageId) => new ($"{Scheme}://{WingetCatalogURIName}/{packageId}");

    /// <inheritdoc/>
    public Uri CreateMsStoreCatalogPackageUri(string packageId) => new ($"{Scheme}://{MsStoreCatalogURIName}/{packageId}");

    /// <inheritdoc/>
    public Uri CreateCustomCatalogPackageUri(string packageId, string catalogName) => new ($"{Scheme}://{catalogName}/{packageId}");

    /// <inheritdoc/>
    public Uri CreatePackageUri(string packageId, WinGetCatalog catalog)
    {
        return catalog.Type switch
        {
            WinGetCatalog.CatalogType.PredefinedWinget => CreateWinGetCatalogPackageUri(packageId),
            WinGetCatalog.CatalogType.PredefinedMsStore => CreateMsStoreCatalogPackageUri(packageId),
            WinGetCatalog.CatalogType.CustomSearch => throw new ArgumentException("Custom search catalog is an invalid input argument"),
            _ => CreateCustomCatalogPackageUri(packageId, catalog.UnknownCatalogName),
        };
    }
}
