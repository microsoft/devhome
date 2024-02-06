// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet;

/// <summary>
/// Thread-safe cache for packages
/// </summary>
internal sealed class WinGetPackageCache : IWinGetPackageCache
{
    private readonly Dictionary<Uri, IWinGetPackage> _cache = new ();
    private readonly object _lock = new ();

    /// <inheritdoc />
    public IList<IWinGetPackage> GetPackages(IEnumerable<Uri> packageUris, out IEnumerable<Uri> packageUrisNotFound)
    {
        // Lock to ensure all packages fetched are from the same cache state
        lock (_lock)
        {
            var foundPackages = new List<IWinGetPackage>();
            var notFoundPackageUris = new List<Uri>();

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
    public bool TryGetPackage(Uri packageUri, out IWinGetPackage package)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(packageUri, out package))
            {
                return true;
            }

            package = null;
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryAddPackage(Uri packageUri, IWinGetPackage package)
    {
        lock (_lock)
        {
            return _cache.TryAdd(packageUri, package);
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
}
