// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Common.Exceptions;
using Windows.Management.Deployment;

namespace DevHome.Common.Services;

public class PackageDeploymentService : IPackageDeploymentService
{
    private readonly PackageManager _packageManager = new ();

    /// <inheritdoc />
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
}
