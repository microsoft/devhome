// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Services.Core.Models;
using Windows.ApplicationModel;

namespace DevHome.Services.Core.Contracts;

/// <summary>
/// Interface for using the deployment API
/// <see cref="Windows.Management.Deployment.PackageManager"/>
/// </summary>
public interface IPackageDeploymentService
{
    /// <summary>
    /// Register a package for the current user
    /// </summary>
    /// <param name="packageFamilyName">Package family name</param>
    /// <param name="options">Register package options</param>
    /// <exception cref="RegisterPackageException">Exception thrown if registration failed</exception>
    public Task RegisterPackageForCurrentUserAsync(string packageFamilyName, RegisterPackageOptions options = null);

    /// <summary>
    /// Find packages for the current user. If maxVersion is specified, package versions must be
    /// between minVersion and maxVersion. If maxVersion is null, packages must be above minVersion.
    /// If no minVersion is specified, returns packages of any version.
    /// </summary>
    /// <returns>An IEnumerable containing the installed packages that meet the version criteria.</returns>
    public IEnumerable<Package> FindPackagesForCurrentUser(string packageFamilyName, params (Version minVersion, Version maxVersion)[] ranges);
}
