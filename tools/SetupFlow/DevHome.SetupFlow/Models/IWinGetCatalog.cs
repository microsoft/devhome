// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.SetupFlow.Exceptions;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Interface for a winget catalog
/// </summary>
public interface IWinGetCatalog
{
    /// <summary>
    /// Gets a value indicating whether a catalog was opened.
    /// <seealso cref="ConnectAsync"/>
    /// </summary>
    public bool IsConnected
    {
        get;
    }

    /// <summary>
    /// Opens a catalog before searching.
    /// This method calls <c>ConnectAsync()</c> from <c>PackageManager.idl</c>
    /// </summary>
    /// <param name="force">Force connect</param>
    /// <exception cref="CatalogConnectionException">Exception thrown if a catalog connection failed</exception>
    public Task ConnectAsync(bool force = false);

    /// <summary>
    /// Search for packages in this catalog.
    /// Equivalent to <c>"winget search --query {query} --source {this}"</c>
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="limit">Maximum number of results to return. Use 0 for infinite results</param>
    /// <returns>List of winget package matches</returns>
    /// <exception cref="FindPackagesException">Exception thrown if the search packages operation failed</exception>
    public Task<IList<IWinGetPackage>> SearchAsync(string query, uint limit = 0);

    /// <summary>
    /// Get packages by id from this catalog.
    /// Equivalent to <c>"winget search --id {packageId} --exact --source {this}"</c>
    /// </summary>
    /// <param name="packageIdSet">Set of package id</param>
    /// <returns>List of winget package matches</returns>
    /// <exception cref="FindPackagesException">Exception thrown if the get packages operation failed</exception>
    public Task<IList<IWinGetPackage>> GetPackagesAsync(ISet<string> packageIdSet);
}
