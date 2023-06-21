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

        // If registration failed, throw an exception with the failure text and inner exception.
        // Note: This also makes the code more testable as DeploymentResult
        // type returned by the original register method cannot be mocked.
        if (!result.IsRegistered)
        {
            throw new RegisterPackageException(result.ErrorText, result.ExtendedErrorCode);
        }
    }
}
