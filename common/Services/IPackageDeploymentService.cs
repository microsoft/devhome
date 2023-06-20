// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace DevHome.Common.Services;

/// <summary>
/// Delegate for a package version condition
/// </summary>
/// <param name="version">Package version</param>
public delegate bool PackageVersionCondition(PackageVersion version);

public interface IPackageDeploymentService
{
    public Task<bool> IsPackageFoundForCurrentUserAsync(string packageFamilyName, PackageVersionCondition? versionCondition = null);

    public Task<IReadOnlyCollection<PackageVersion>> GetPackageVersionsForCurrentUserAsync(string packageFamilyName);

    public Task RegisterPackageForCurrentUserAsync(string packageFamilyName, RegisterPackageOptions? options = null);
}

public class RegisterPackageOptions
{
    public IEnumerable<string>? DependencyPackageFamilyNames { get; set; }

    public DeploymentOptions? DeploymentOptions { get; set; }

    public PackageVolume? AppDataVolume { get; set; }

    public IEnumerable<string>? OptionalPackageFamilyNames { get; set; }
}
