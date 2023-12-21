// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet;

public record class WinGetProtocolParserResult(string packageId, string catalogUriName);

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
    public WinGetProtocolParserResult ParsePackageUri(Uri packageUri)
    {
        if (packageUri.Scheme == Scheme && packageUri.Segments.Length == 2)
        {
            var packageId = packageUri.Segments[1];
            var catalogUriName = packageUri.Host;
            return new (packageId, catalogUriName);
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<WinGetCatalog> ResolveCatalogAsync(WinGetProtocolParserResult result)
    {
        var catalogName = result.catalogUriName;

        // 'winget' catalog
        if (catalogName == WingetCatalogURIName)
        {
            return await _catalogConnector.GetPredefinedWingetCatalogAsync();
        }

        // 'msstore' catalog
        if (catalogName == MsStoreCatalogURIName)
        {
            return await _catalogConnector.GetPredefinedMsStoreCatalogAsync();
        }

        // custom catalog
        return await _catalogConnector.GetPackageCatalogByNameAsync(catalogName);
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
