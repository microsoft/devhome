// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Services.Core.Contracts;
using DevHome.Services.Core.Exceptions;
using DevHome.Services.Core.Models;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel;

namespace DevHome.Services.Core.Services;

public class PackageDeploymentService : IPackageDeploymentService
{
    private readonly ILogger _logger;
    private readonly Windows.Management.Deployment.PackageManager _packageManager;

    public PackageDeploymentService(ILogger<PackageDeploymentService> logger)
    {
        _logger = logger;
        _packageManager = new();
    }

    /// <inheritdoc />
    public async Task RegisterPackageForCurrentUserAsync(string packageFamilyName, RegisterPackageOptions options = null)
    {
        var result = await _packageManager.RegisterPackageByFamilyNameAsync(
            packageFamilyName,
            options?.DependencyPackageFamilyNames ?? new List<string>(),
            options?.DeploymentOptions ?? Windows.Management.Deployment.DeploymentOptions.None,
            options?.AppDataVolume,
            options?.OptionalPackageFamilyNames ?? new List<string>());

        // If registration failed, throw an exception with the failure text and inner exception.
        // Note: This also makes the code more testable as DeploymentResult
        // type returned by the original register method cannot be mocked.
        if (!result.IsRegistered)
        {
            throw new RegisterPackageException(result.ErrorText, result.ExtendedErrorCode);
        }
    }

    /// <inheritdoc />
    public IEnumerable<Package> FindPackagesForCurrentUser(string packageFamilyName, params (Version minVersion, Version maxVersion)[] ranges)
    {
        var packages = _packageManager.FindPackagesForUser(string.Empty, packageFamilyName);
        if (packages.Any())
        {
            var versionedPackages = new List<Package>();
            foreach (var package in packages)
            {
                var version = package.Id.Version;
                var major = version.Major;
                var minor = version.Minor;
                var build = version.Build;
                var revision = version.Revision;

                _logger.LogInformation($"Found package {package.Id.FullName}");

                // Create System.Version type from PackageVersion to test. System.Version supports CompareTo() for easy comparisons.
                if (IsVersionSupported(new(major, minor, build, revision), ranges))
                {
                    versionedPackages.Add(package);
                }
            }

            return versionedPackages;
        }
        else
        {
            // If there is no version installed at all, return the empty enumerable.
            _logger.LogInformation($"Found no installed version of {packageFamilyName}");
            return packages;
        }
    }

    /// <summary>
    /// Tests whether a version is equal to or above the min, but less than the max.
    /// </summary>
    private bool IsVersionBetween(Version target, Version min, Version max) => target.CompareTo(min) >= 0 && target.CompareTo(max) < 0;

    /// <summary>
    /// Tests whether a version is equal to or above the min.
    /// </summary>
    private bool IsVersionAtOrAbove(Version target, Version min) => target.CompareTo(min) >= 0;

    private bool IsVersionSupported(Version target, params (Version minVersion, Version maxVersion)[] ranges)
    {
        // If a min version wasn't specified, any version is fine.
        if (ranges.Length == 0)
        {
            return true;
        }

        foreach (var (minVersion, maxVersion) in ranges)
        {
            if (maxVersion == null)
            {
                if (IsVersionAtOrAbove(target, minVersion))
                {
                    return true;
                }
            }
            else
            {
                if (IsVersionBetween(target, minVersion, maxVersion))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
