// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Windows.Management.Deployment;

namespace DevHome.Services.Core.Models;

/// <summary>
/// Parameter object for <see cref="IPackageDeploymentService.RegisterPackageForCurrentUserAsync"/>
/// More details: https://learn.microsoft.com/uwp/api/windows.management.deployment.packagemanager.registerpackagebyfamilynameasync?view=winrt-22621
/// </summary>
public sealed class RegisterPackageOptions
{
    /// <summary>
    /// Gets or sets the family names of the dependency packages to be registered.
    /// </summary>
    public IEnumerable<string> DependencyPackageFamilyNames { get; set; }

    /// <summary>
    /// Gets or sets the package deployment option.
    /// </summary>
    public DeploymentOptions DeploymentOptions { get; set; }

    /// <summary>
    /// Gets or sets the package volume to store that app data on.
    /// </summary>
    public PackageVolume AppDataVolume { get; set; }

    /// <summary>
    /// Gets or sets the optional package family names from the main bundle to be registered.
    /// </summary>
    public IEnumerable<string> OptionalPackageFamilyNames { get; set; }
}
