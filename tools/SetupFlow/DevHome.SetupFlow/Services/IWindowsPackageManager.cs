// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Interface for interacting with the WinGet package manager.
/// More details: https://github.com/microsoft/winget-cli/blob/master/src/Microsoft.Management.Deployment/PackageManager.idl
/// </summary>
public interface IWindowsPackageManager
{
    /// <summary>
    /// Initialize the winget package manager.
    /// </summary>
    public Task InitializeAsync();

    /// <summary>
    /// Install a package on the user's machine.
    /// </summary>
    /// <param name="package">Package to install</param>
    /// <returns>Install package result</returns>
    public Task<InstallPackageResult> InstallPackageAsync(IWinGetPackage package);

    /// <summary>
    /// Checks if AppInstaller has an available update
    /// </summary>
    /// <returns>True if an AppInstaller update is available, false otherwise</returns>
    public Task<bool> IsUpdateAvailableAsync();

    /// <summary>
    /// Check whether the WindowsPackageManagerServer is available to create
    /// out-of-proc COM objects
    /// </summary>
    /// <returns>True if COM Server is available, false otherwise</returns>
    public Task<bool> IsAvailableAsync();

    /// <summary>
    /// Register AppInstaller
    /// </summary>
    /// <returns>True if AppInstaller was registered, false otherwise.</returns>
    public Task<bool> RegisterAppInstallerAsync();

    /// <summary>
    /// Get packages from a set of package uri.
    /// </summary>
    /// <param name="packageUriSet">Set of package uri</param>
    /// <returns>List of winget package matches</returns>
    /// <exception cref="FindPackagesException">Exception thrown if the get packages operation failed</exception>
    public Task<IList<IWinGetPackage>> GetPackagesAsync(ISet<Uri> packageUriSet);

    /// <summary>
    /// Search for packages in this catalog.
    /// Equivalent to <c>"winget search --query {query} --source {this}"</c>
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="limit">Maximum number of results to return. Use 0 for infinite results</param>
    /// <returns>List of winget package matches</returns>
    /// <exception cref="FindPackagesException">Exception thrown if the search packages operation failed</exception>
    public Task<IList<IWinGetPackage>> SearchAsync(string query, uint limit);

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
}
