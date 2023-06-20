// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Management.Deployment;

namespace DevHome.Common.Services;

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
    public Task RegisterPackageForCurrentUserAsync(string packageFamilyName, RegisterPackageOptions? options = null);
}

/// <summary>
/// Parameter object for <see cref="IPackageDeploymentService.RegisterPackageForCurrentUserAsync"/>
/// More details: https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.packagemanager.registerpackagebyfamilynameasync?view=winrt-22621
/// </summary>
public sealed class RegisterPackageOptions
{
    /// <summary>
    /// Gets or sets the family names of the dependency packages to be registered.
    /// </summary>
    public IEnumerable<string>? DependencyPackageFamilyNames { get; set; }

    /// <summary>
    /// Gets or sets the package deployment option.
    /// </summary>
    public DeploymentOptions? DeploymentOptions { get; set; }

    /// <summary>
    /// Gets or sets the package volume to store that app data on.
    /// </summary>
    public PackageVolume? AppDataVolume { get; set; }

    /// <summary>
    /// Gets or sets the optional package family names from the main bundle to be registered.
    /// </summary>
    public IEnumerable<string>? OptionalPackageFamilyNames { get; set; }
}
