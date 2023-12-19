// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet;

public interface IWinGetCatalogConnector
{
    /// <summary>
    /// Get the predefined 'winget' catalog
    /// </summary>
    /// <returns>Winget catalog or null if not initialized</returns>
    public Task<WinGetCatalog> GetPredefinedWingetCatalogAsync();

    /// <summary>
    /// Get the predefined 'msstore' catalog
    /// </summary>
    /// <returns>Microsoft store catalog or null if not initialized</returns>
    public Task<WinGetCatalog> GetPredefinedMsStoreCatalogAsync();

    /// <summary>
    /// Get the custom search catalog
    /// </summary>
    /// <returns>Search catalog or null if not initialized</returns>
    public Task<WinGetCatalog> GetCustomSearchCatalogAsync();

    /// <summary>
    /// Get the corresponding catalog for the provided package
    /// </summary>
    /// <param name="package">Target package</param>
    /// <returns>Catalog for the provided package or null if corresponding catalog is not initialized</returns>
    public Task<WinGetCatalog> GetPackageCatalogAsync(IWinGetPackage package);

    /// <summary>
    /// Get a catalog by its name
    /// </summary>
    /// <param name="catalogName">Catalog name</param>
    /// <returns>Catalog or null if an error occurred during initialization</returns>
    public Task<WinGetCatalog> GetPackageCatalogByNameAsync(string catalogName);

    /// <summary>
    /// Check if the provided package is a 'msstore' package
    /// </summary>
    /// <param name="package">Target package</param>
    /// <returns>True if the provided package is a 'msstore' package</returns>
    public bool IsMsStorePackage(IWinGetPackage package);

    /// <summary>
    /// Check if the provided package is a 'winget' package
    /// </summary>
    /// <param name="package">Target package</param>
    /// <returns>True if the provided package is a 'winget' package</returns>
    public bool IsWinGetPackage(IWinGetPackage package);

    /// <summary>
    /// Check if the provided catalog is alive
    /// </summary>
    /// <param name="catalog">Target catalog</param>
    /// <returns>True if the catalog is alive</returns>
    public bool IsCatalogAlive(WinGetCatalog catalog);

    /// <summary>
    /// Create and connect to all catalogs
    /// </summary>
    public Task CreateAndConnectCatalogsAsync();
}
