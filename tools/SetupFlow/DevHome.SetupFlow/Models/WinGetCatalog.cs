// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using WPMPackageCatalog = Microsoft.Management.Deployment.PackageCatalog;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Internal model for a WinGet catalog.
/// </summary>
internal class WinGetCatalog
{
    public enum CatalogType
    {
        PredefinedWinget,
        PredefinedMsStore,
        CustomSearch,
        CustomUnknown,
    }

    /// <summary>
    /// Gets the catalog object.
    /// </summary>
    public WPMPackageCatalog Catalog { get; }

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

    public WinGetCatalog(WPMPackageCatalog catalog, CatalogType type, string unknownCatalogName = null)
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
