// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services;

public interface ICatalogDataSourceLoader
{
    /// <summary>
    /// Gets the total number of catalogs from all data sources
    /// </summary>
    public int CatalogCount { get; }

    /// <summary>
    /// Initialize all data sources
    /// </summary>
    public Task InitializeAsync();

    /// <summary>
    /// Load catalogs from all data sources
    /// </summary>
    /// <returns>Catalogs from all data sources</returns>
    public IAsyncEnumerable<IList<PackageCatalog>> LoadCatalogsAsync();
}
