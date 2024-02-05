// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet.Operations;

/// <summary>
/// Get packages using WinGet with recovery
/// </summary>
internal sealed class WinGetGetPackageOperation : IWinGetGetPackageOperation
{
    private readonly IWinGetPackageCache _packageCache;
    private readonly IWinGetProtocolParser _protocolParser;
    private readonly IWinGetPackageFinder _packageFinder;
    private readonly IWinGetRecovery _recovery;

    public WinGetGetPackageOperation(
        IWinGetPackageCache packageCache,
        IWinGetProtocolParser protocolParser,
        IWinGetPackageFinder packageFinder,
        IWinGetRecovery recovery)
    {
        _packageCache = packageCache;
        _protocolParser = protocolParser;
        _packageFinder = packageFinder;
        _recovery = recovery;
    }

    /// <inheritdoc />
    public async Task<IList<IWinGetPackage>> GetPackagesAsync(IList<Uri> packageUris)
    {
        // Remove duplicates (optimization to prevent querying the same package multiple times)
        var distinctPackageUris = packageUris.Distinct();

        // Find packages in the cache and packages that need to be queried
        var cachedPackages = _packageCache.GetPackages(distinctPackageUris, out var packageUrisToQuery);
        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Packages loaded from cache [{string.Join(", ", cachedPackages.Select(p => $"({p.Id}, {p.CatalogName})"))}]");
        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Package URIs not found in cache [{string.Join(", ", packageUrisToQuery)}]");

        // Get packages grouped by catalog
        var getPackagesTasks = new List<Task<List<IWinGetPackage>>>();
        foreach (var parsedUrisGroup in GroupParsedUrisByCatalog(packageUrisToQuery))
        {
            if (parsedUrisGroup.Count != 0)
            {
                // Get packages from each catalog concurrently
                getPackagesTasks.Add(_recovery.DoWithRecoveryAsync(async () =>
                {
                    // All parsed URIs in the group have the same catalog, resolve catalog from the first entry
                    var firstParsedUri = parsedUrisGroup.First();
                    var packageIds = parsedUrisGroup.Select(p => p.PackageId).ToHashSet();
                    Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Getting packages [{string.Join(", ", packageIds)}] from parsed uri catalog name: {firstParsedUri.CatalogUriName}");

                    // Get packages from the catalog
                    var catalog = await _protocolParser.ResolveCatalogAsync(firstParsedUri);
                    var packagesOutOfProc = await _packageFinder.GetPackagesAsync(catalog, packageIds);
                    var packagesInProc = packagesOutOfProc
                        .Select(p => new WinGetPackage(p, _packageFinder.IsElevationRequired(p)))
                        .ToList<IWinGetPackage>();
                    packagesInProc
                        .ForEach(p => _packageCache.TryAddPackage(_protocolParser.CreatePackageUri(p), p));
                    return packagesInProc;
                }));
            }
        }

        // Wait for all packages to be retrieved
        await Task.WhenAll(getPackagesTasks);
        var unorderedPackagesMap = getPackagesTasks
            .SelectMany(p => p.Result)
            .Concat(cachedPackages)
            .ToDictionary(p => _protocolParser.CreatePackageUri(p), p => p);

        // Order packages by the order of the input URIs using a dictionary
        var orderedPackages = new List<IWinGetPackage>();
        foreach (var packageUri in packageUris)
        {
            if (unorderedPackagesMap.TryGetValue(packageUri, out var package))
            {
                orderedPackages.Add(package);
            }
            else
            {
                Log.Logger?.ReportWarn(Log.Component.AppManagement, $"Failed to find package URI '{packageUri}'");
            }
        }

        return orderedPackages;
    }

    /// <summary>
    /// Group packages by their catalogs
    /// </summary>
    /// <param name="packageUriSet">Package URIs</param>
    /// <returns>Dictionary of package ids by catalog</returns>
    private List<List<WinGetProtocolParserResult>> GroupParsedUrisByCatalog(IEnumerable<Uri> packageUriSet)
    {
        var parsedUris = new List<WinGetProtocolParserResult>();

        // 1. Parse all package URIs and log invalid ones
        foreach (var packageUri in packageUriSet)
        {
            var uriInfo = _protocolParser.ParsePackageUri(packageUri);
            if (uriInfo != null)
            {
                parsedUris.Add(uriInfo);
            }
            else
            {
                Log.Logger?.ReportWarn(Log.Component.AppManagement, $"Failed to get URI details from '{packageUri}'");
            }
        }

        // 2. Group package ids by catalog
        return parsedUris.GroupBy(p => p.CatalogUriName).Select(p => p.ToList()).ToList();
    }
}
