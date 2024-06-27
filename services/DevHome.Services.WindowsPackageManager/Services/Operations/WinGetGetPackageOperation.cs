// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Services.WindowsPackageManager.Contracts;
using DevHome.Services.WindowsPackageManager.Contracts.Operations;
using DevHome.Services.WindowsPackageManager.Models;
using Microsoft.Extensions.Logging;

namespace DevHome.Services.WindowsPackageManager.Services.Operations;

/// <summary>
/// Get packages using WinGet with recovery
/// </summary>
internal sealed class WinGetGetPackageOperation : IWinGetGetPackageOperation
{
    private readonly ILogger _logger;
    private readonly IWinGetPackageCache _packageCache;
    private readonly IWinGetProtocolParser _protocolParser;
    private readonly IWinGetPackageFinder _packageFinder;
    private readonly IWinGetRecovery _recovery;

    public WinGetGetPackageOperation(
        ILogger<WinGetGetPackageOperation> logger,
        IWinGetPackageCache packageCache,
        IWinGetProtocolParser protocolParser,
        IWinGetPackageFinder packageFinder,
        IWinGetRecovery recovery)
    {
        _logger = logger;
        _packageCache = packageCache;
        _protocolParser = protocolParser;
        _packageFinder = packageFinder;
        _recovery = recovery;
    }

    /// <inheritdoc />
    public async Task<IList<IWinGetPackage>> GetPackagesAsync(IList<WinGetPackageUri> packageUris)
    {
        // Remove duplicates (optimization to prevent querying the same package multiple times)
        var distinctPackageUris = packageUris.Distinct();

        // Find packages in the cache and packages that need to be queried
        var cachedPackages = _packageCache.GetPackages(distinctPackageUris, out var packageUrisToQuery);
        _logger.LogInformation($"Packages loaded from cache [{string.Join(", ", cachedPackages.Select(p => $"({p.Id}, {p.CatalogName})"))}]");
        _logger.LogInformation($"Package URIs not found in cache [{string.Join(", ", packageUrisToQuery)}]");

        // Get packages grouped by catalog
        var getPackagesTasks = new List<Task<List<IWinGetPackage>>>();
        var groupedParsedUris = packageUrisToQuery.GroupBy(p => p.CatalogName).Select(p => p.ToList()).ToList();
        foreach (var parsedUrisGroup in groupedParsedUris)
        {
            if (parsedUrisGroup.Count != 0)
            {
                // Get packages from each catalog concurrently
                getPackagesTasks.Add(_recovery.DoWithRecoveryAsync(async () =>
                {
                    // All parsed URIs in the group have the same catalog, resolve catalog from the first entry
                    var firstParsedUri = parsedUrisGroup.First();
                    var packageIds = parsedUrisGroup.Select(p => p.PackageId).ToHashSet();
                    _logger.LogInformation($"Getting packages [{string.Join(", ", packageIds)}] from parsed uri catalog name: {firstParsedUri.CatalogName}");

                    // Get packages from the catalog
                    var catalog = await _protocolParser.ResolveCatalogAsync(firstParsedUri);
                    var packagesOutOfProc = await _packageFinder.GetPackagesAsync(catalog, packageIds);
                    var packagesInProc = packagesOutOfProc
                        .Select(p => new WinGetPackage(_logger, p, _packageFinder.IsElevationRequired(p)))
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
            .ToDictionary(p => _protocolParser.CreatePackageUri(p).ToString(WinGetPackageUriParameters.None), p => p);

        // Order packages by the order of the input URIs using a dictionary
        var orderedPackages = new List<IWinGetPackage>();
        foreach (var packageUri in packageUris)
        {
            if (unorderedPackagesMap.TryGetValue(packageUri.ToString(WinGetPackageUriParameters.None), out var package))
            {
                orderedPackages.Add(package);
            }
            else
            {
                _logger.LogWarning($"Failed to find package URI '{packageUri}'");
            }
        }

        return orderedPackages;
    }
}
