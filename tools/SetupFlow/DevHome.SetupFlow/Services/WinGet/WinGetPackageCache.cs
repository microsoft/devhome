// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet;

/// <summary>
/// Thread-safe cache for packages
/// </summary>
internal sealed class WinGetPackageCache : IWinGetPackageCache
{
    private readonly Dictionary<string, IWinGetPackage> _cache = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public IList<IWinGetPackage> GetPackages(IEnumerable<WinGetPackageUri> packageUris, out IEnumerable<WinGetPackageUri> packageUrisNotFound)
    {
        // Lock to ensure all packages fetched are from the same cache state
        lock (_lock)
        {
            var foundPackages = new List<IWinGetPackage>();
            var notFoundPackageUris = new List<WinGetPackageUri>();

            foreach (var packageUri in packageUris)
            {
                if (TryGetPackage(packageUri, out var foundPackage))
                {
                    foundPackages.Add(foundPackage);
                }
                else
                {
                    notFoundPackageUris.Add(packageUri);
                }
            }

            packageUrisNotFound = notFoundPackageUris;
            return foundPackages;
        }
    }

    /// <inheritdoc />
    public bool TryGetPackage(WinGetPackageUri packageUri, out IWinGetPackage package)
    {
        lock (_lock)
        {
            var key = CreateKey(packageUri);
            if (_cache.TryGetValue(key, out package))
            {
                return true;
            }

            package = null;
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryAddPackage(WinGetPackageUri packageUri, IWinGetPackage package)
    {
        lock (_lock)
        {
            var key = CreateKey(packageUri);
            return _cache.TryAdd(key, package);
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
        }
    }

    /// <summary>
    /// Create a key from a package URI
    /// </summary>
    /// <param name="packageUri">Package URI</param>
    /// <returns>Unique key from a package URI</returns>
    private string CreateKey(WinGetPackageUri packageUri)
    {
        return packageUri.ToString(WinGetPackageUriParameters.None);
    }
}
