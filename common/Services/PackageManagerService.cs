// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using static DevHome.Common.Services.IPackageManagerService;

namespace DevHome.Common.Services;

public class PackageManagerService : IPackageManagerService
{
    private readonly PackageManager _packageManager = new ();

    public async Task<bool> IsInstalledAsync(string packageFamilyName, PackageVersionCondition? versionCondition)
    {
        var result = await FindInstalledPackagesAsync(packageFamilyName);
        return result.Any(package => versionCondition?.Invoke(package.Id.Version) ?? true);
    }

    private async Task<IEnumerable<Package>> FindInstalledPackagesAsync(string packageFamilyName)
    {
        return await Task.Run(() => _packageManager.FindPackagesForUser(string.Empty, packageFamilyName));
    }
}
