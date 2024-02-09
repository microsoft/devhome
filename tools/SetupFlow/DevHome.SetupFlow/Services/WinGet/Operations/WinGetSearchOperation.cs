// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet.Operations;

/// <summary>
/// Search for packages using WinGet with recovery
/// </summary>
internal sealed class WinGetSearchOperation : IWinGetSearchOperation
{
    private readonly IWinGetCatalogConnector _catalogConnector;
    private readonly IWinGetPackageFinder _packageFinder;
    private readonly IWinGetRecovery _recovery;

    public WinGetSearchOperation(
        IWinGetCatalogConnector catalogConnector,
        IWinGetPackageFinder packageFinder,
        IWinGetRecovery recovery)
    {
        _catalogConnector = catalogConnector;
        _packageFinder = packageFinder;
        _recovery = recovery;
    }

    /// <inheritdoc />
    public async Task<IList<IWinGetPackage>> SearchAsync(string query, uint limit)
    {
        return await _recovery.DoWithRecoveryAsync(async () =>
        {
            var searchCatalog = await _catalogConnector.GetCustomSearchCatalogAsync();
            var results = await _packageFinder.SearchAsync(searchCatalog, query, limit);
            return results
                .Select(p => new WinGetPackage(p, _packageFinder.IsElevationRequired(p)))
                .ToList<IWinGetPackage>();
        });
    }
}
