// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using WPMPackageCatalog = Microsoft.Management.Deployment.PackageCatalog;

namespace DevHome.SetupFlow.Models;

public class WinGetCatalog
{
    public enum CatalogType
    {
        PredefinedWinget,
        PredefinedMsStore,
        CustomSearch,
        CustomUnknown,
    }

    public WPMPackageCatalog Catalog { get; }

    public CatalogType Type { get; }

    public string UnknownCatalogName { get; }

    public WinGetCatalog(WPMPackageCatalog catalog, CatalogType type, string unknownCatalogName = null)
    {
        Catalog = catalog;
        Type = type;
        UnknownCatalogName = unknownCatalogName;
    }

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
