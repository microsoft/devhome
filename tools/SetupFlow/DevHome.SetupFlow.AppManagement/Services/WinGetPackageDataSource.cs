// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.SetupFlow.AppManagement.Models;

namespace DevHome.SetupFlow.AppManagement.Services;
public abstract class WinGetPackageDataSource
{
    private readonly IWindowsPackageManager _wpm;

    public abstract int CatalogCount
    {
        get;
    }

    public WinGetPackageDataSource(IWindowsPackageManager wpm)
    {
        _wpm = wpm;
    }

    public abstract Task<IList<PackageCatalog>> LoadCatalogsAsync();

    protected async Task<IList<IWinGetPackage>> GetOrderedPackagesAsync<T>(
        IList<T> items,
        Func<T, string> packageIdFunc,
        Func<IWinGetPackage, T, Task> packageProcessorFunc = null)
    {
        List<IWinGetPackage> result = new ();

        // Get packages from winget catalog
        var unorderedPackages = await _wpm.WinGetCatalog.GetPackagesAsync(items.Select(packageIdFunc).ToHashSet());
        var unorderedPackagesMap = unorderedPackages.ToDictionary(p => p.Id, p => p);

        // Sort result based on the input and set images
        foreach (var item in items)
        {
            var package = unorderedPackagesMap.GetValueOrDefault(packageIdFunc(item), null);
            if (package != null)
            {
                if (packageProcessorFunc != null)
                {
                    await packageProcessorFunc(package, item);
                }

                result.Add(package);
            }
        }

        return result;
    }
}
