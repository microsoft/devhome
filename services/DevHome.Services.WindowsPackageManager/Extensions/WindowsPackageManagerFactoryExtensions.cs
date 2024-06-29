// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using DevHome.Services.WindowsPackageManager.COM;
using Microsoft.Management.Deployment;

namespace DevHome.Services.WindowsPackageManager.Extensions;

internal static class WindowsPackageManagerFactoryExtensions
{
    /// <summary>
    /// Creates a new instance of <see cref="PackageMatchFilter"/> with the specified values.
    /// </summary>
    /// <param name="factory">Extension method target instance.</param>
    /// <param name="field">Field to match.</param>
    /// <param name="option">Match option.</param>
    /// <param name="value">Value to match.</param>
    /// <returns>Instance of <see cref="PackageMatchFilter"/>.</returns>
    public static PackageMatchFilter CreatePackageMatchFilter(
        this WindowsPackageManagerFactory factory,
        PackageMatchField field,
        PackageFieldMatchOption option,
        string value)
    {
        var filter = factory.CreatePackageMatchFilter();
        filter.Field = field;
        filter.Option = option;
        filter.Value = value;
        return filter;
    }

    /// <summary>
    /// Creates a new instance of <see cref="PackageMatchFilter"/> with the specified values.
    /// </summary>
    /// <param name="factory">Extension method target instance.</param>
    /// <param name="behavior">Search behavior.</param>
    /// <param name="catalogReferences">Catalog references.</param>
    /// <returns>Instance of <see cref="CreateCompositePackageCatalogOptions"/>.</returns>
    public static PackageCatalogReference CreateCompositePackageCatalog(
        this WindowsPackageManagerFactory factory,
        CompositeSearchBehavior behavior,
        IReadOnlyList<PackageCatalogReference> catalogReferences)
    {
        var compositeCatalogOptions = factory.CreateCreateCompositePackageCatalogOptions();
        compositeCatalogOptions.CompositeSearchBehavior = behavior;

        // Add all catalogs to the new composite catalog
        // Note: Cannot use foreach or LINQ for out-of-process IVector
        // Bug: https://github.com/microsoft/CsWinRT/issues/1205
        var count = catalogReferences.Count;
        for (var i = 0; i < count; ++i)
        {
            compositeCatalogOptions.Catalogs.Add(catalogReferences[i]);
        }

        var packageManager = factory.CreatePackageManager();
        return packageManager.CreateCompositePackageCatalog(compositeCatalogOptions);
    }
}
