// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet;

internal class WinGetPackageCache : IWinGetPackageCache
{
    private readonly IWinGetProtocolParser _protocolParser;
    private readonly Dictionary<Uri, IWinGetPackage> _cache = new ();
    private readonly object _lock = new ();

    public WinGetPackageCache(IWinGetProtocolParser protocolParser)
    {
        _protocolParser = protocolParser;
    }

    /// <inheritdoc />
    public IList<IWinGetPackage> GetPackages(IEnumerable<Uri> packageUris, out IEnumerable<Uri> packageUrisNotFound)
    {
        lock (_lock)
        {
            var foundPackages = new List<IWinGetPackage>();
            var notFoundPackageUris = new List<Uri>();

            foreach (var packageUri in packageUris)
            {
                if (_cache.TryGetValue(packageUri, out var foundPackage))
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
    public bool TryAddPackage(IWinGetPackage package)
    {
        lock (_cache)
        {
            return _cache.TryAdd(_protocolParser.CreatePackageUri(package), package);
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
