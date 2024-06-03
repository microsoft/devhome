// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet;

/// <summary>
/// Thread -safe cache for packages
/// </summary>
internal interface IWinGetPackageCache
{
    /// <summary>
    /// Get packages from the cache.
    /// </summary>
    /// <param name="packageUris">Package URIs to find</param>
    /// <param name="packageUrisNotFound">Output package URIs not found</param>
    /// <returns>List of packages found</returns>
    public IList<IWinGetPackage> GetPackages(IEnumerable<WinGetPackageUri> packageUris, out IEnumerable<WinGetPackageUri> packageUrisNotFound);

    /// <summary>
    /// Try to get a package in the cache.
    /// </summary>
    /// <param name="packageUri">Package URI to find</param>
    /// <param name="package">Output package</param>
    /// <returns>True if the package was found, false otherwise.</returns>
    public bool TryGetPackage(WinGetPackageUri packageUri, out IWinGetPackage package);

    /// <summary>
    /// Try to add a package to the cache.
    /// </summary>
    /// <param name="packageUri">Package URI to add</param>
    /// <param name="package">Package to add</param>
    /// <returns>True if the package was added, false otherwise.</returns>
    public bool TryAddPackage(WinGetPackageUri packageUri, IWinGetPackage package);

    /// <summary>
    /// Clear the cache.
    /// </summary>
    public void Clear();
}
