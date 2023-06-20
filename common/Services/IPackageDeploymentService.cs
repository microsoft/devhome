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
/// More details: <seealso cref="PackageManager.RegisterPackageByFamilyNameAsync"/>
/// </summary>
public sealed class RegisterPackageOptions
{
    public IEnumerable<string>? DependencyPackageFamilyNames { get; set; }

    public DeploymentOptions? DeploymentOptions { get; set; }

    public PackageVolume? AppDataVolume { get; set; }

    public IEnumerable<string>? OptionalPackageFamilyNames { get; set; }
}
