// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Management.Deployment;

namespace DevHome.Services.WindowsPackageManager.Models;

/// <summary>
/// Internal model for a WinGet catalog.
/// </summary>
internal sealed class WinGetCatalog
{
    public enum CatalogType
    {
        // 'winget' source is a predefined catalog that is built-in to WinGet.
        PredefinedWinget,

        // 'msstore' source is a predefined catalog that is built-in to WinGet.
        PredefinedMsStore,

        // A custom catalog used for searching in all catalogs.
        CustomSearch,

        // A custom non-predefined catalog.
        CustomUnknown,
    }

    /// <summary>
    /// Gets the catalog object.
    /// </summary>
    public PackageCatalog Catalog { get; }

    /// <summary>
    /// Gets the type of the catalog.
    /// </summary>
    public CatalogType Type { get; }

    /// <summary>
    /// Gets the name of the custom catalog.
    /// </summary>
    /// <remarks>
    /// Only used for <see cref="CatalogType.CustomUnknown"/>. For other types, this is <see langword="null"/>.
    /// </remarks>
    public string UnknownCatalogName { get; }

    public WinGetCatalog(PackageCatalog catalog, CatalogType type, string unknownCatalogName = null)
    {
        Catalog = catalog;
        Type = type;
        UnknownCatalogName = unknownCatalogName;
    }

    /// <summary>
    /// Gets a descriptive name for the catalog.
    /// </summary>
    /// <returns>A descriptive name for the catalog.</returns>
    public string GetDescriptiveName()
    {
        return Type switch
        {
            CatalogType.PredefinedWinget => nameof(CatalogType.PredefinedWinget),
            CatalogType.PredefinedMsStore => nameof(CatalogType.PredefinedMsStore),
            CatalogType.CustomSearch => nameof(CatalogType.CustomSearch),
            _ => $"{nameof(CatalogType.CustomUnknown)}: '{UnknownCatalogName}'",
        };
    }
}
