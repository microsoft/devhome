// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet;

internal interface IWinGetProtocolParser
{
    /// <summary>
    /// Create a package uri from a package
    /// </summary>
    /// <param name="package">Package</param>
    /// <returns>Package uri</returns>
    public WinGetPackageUri CreatePackageUri(IWinGetPackage package);

    /// <summary>
    /// Create a winget catalog package uri from a package id
    /// </summary>
    /// <param name="packageId">Package id</param>
    /// <returns>Package uri</returns>
    public WinGetPackageUri CreateWinGetCatalogPackageUri(string packageId);

    /// <summary>
    /// Create a Microsoft store catalog package uri from a package id
    /// </summary>
    /// <param name="packageId">Package id</param>
    /// <returns>Package uri</returns>
    public WinGetPackageUri CreateMsStoreCatalogPackageUri(string packageId);

    /// <summary>
    /// Create a custom catalog package uri from a package id and catalog name
    /// </summary>
    /// <param name="packageId">Package id</param>
    /// <param name="catalogName">Catalog name</param>
    /// <returns>Package uri</returns>
    public WinGetPackageUri CreateCustomCatalogPackageUri(string packageId, string catalogName);

    /// <summary>
    /// Resolve a catalog from a parser result
    /// </summary>
    /// <param name="packageUri">Package uri</param>
    /// <returns>Catalog</returns>
    public Task<WinGetCatalog> ResolveCatalogAsync(WinGetPackageUri packageUri);

    /// <summary>
    /// Create a package uri from a package id and catalog
    /// </summary>
    /// <param name="packageId">Package id</param>
    /// <param name="catalog">Catalog</param>
    /// <returns>Package uri</returns>
    public WinGetPackageUri CreatePackageUri(string packageId, WinGetCatalog catalog);
}
