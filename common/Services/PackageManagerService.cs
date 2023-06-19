// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace DevHome.Common.Services;

public class PackageManagerService : IPackageManagerService
{
    private readonly PackageManager _packageManager = new ();

    public async Task<bool> IsInstalledAsync(string packageFamilyName, PackageVersionCondition? versionCondition)
    {
        var result = await FindInstalledPackagesAsync(packageFamilyName);
        return result.Any(package => versionCondition?.Invoke(package.Id.Version) ?? true);
    }

    public async Task<IReadOnlyCollection<PackageVersion>> GetInstalledVersionsAsync(string packageFamilyName)
    {
        var result = await FindInstalledPackagesAsync(packageFamilyName);
        return result.Select(p => p.Id.Version).ToReadOnlyCollection();
    }

    private async Task<IEnumerable<Package>> FindInstalledPackagesAsync(string packageFamilyName)
    {
        return await Task.Run(() => _packageManager.FindPackagesForUser(string.Empty, packageFamilyName));
    }
}
