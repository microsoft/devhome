// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace DevHome.Common.Services;

public class PackageDeploymentService : IPackageDeploymentService
{
    private readonly PackageManager _packageManager = new ();

    public async Task<bool> IsPackageFoundForCurrentUserAsync(string packageFamilyName, PackageVersionCondition? versionCondition)
    {
        var result = await FindInstalledPackagesForCurrentUserAsync(packageFamilyName);
        return result.Any(package => versionCondition?.Invoke(package.Id.Version) ?? true);
    }

    public async Task<IReadOnlyCollection<PackageVersion>> GetPackageVersionsForCurrentUserAsync(string packageFamilyName)
    {
        var result = await FindInstalledPackagesForCurrentUserAsync(packageFamilyName);
        return result.Select(p => p.Id.Version).ToReadOnlyCollection();
    }

    public async Task RegisterPackageForCurrentUserAsync(string packageFamilyName, RegisterPackageOptions? options = null)
    {
        var result = await _packageManager.RegisterPackageByFamilyNameAsync(
            packageFamilyName,
            options?.DependencyPackageFamilyNames ?? new List<string>(),
            options?.DeploymentOptions ?? DeploymentOptions.None,
            options?.AppDataVolume,
            options?.OptionalPackageFamilyNames ?? new List<string>());

        if (!result.IsRegistered)
        {
            throw new RegisterPackageException(result.ErrorText, result.ExtendedErrorCode);
        }
    }

    private async Task<IEnumerable<Package>> FindInstalledPackagesForCurrentUserAsync(string packageFamilyName)
    {
        var currentUser = string.Empty;
        return await Task.Run(() => _packageManager.FindPackagesForUser(currentUser, packageFamilyName));
    }
}

public class RegisterPackageException : Exception
{
    public RegisterPackageException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
