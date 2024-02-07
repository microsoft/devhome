// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace DevHome.SetupFlow.Models;

// TODO Rename this class to PackageCollection to avoid confusion with the COM PackageCatalog class
// https://github.com/microsoft/devhome/issues/636

/// <summary>
/// Model class for a package catalog. A package catalog contains a list of
/// packages provided from the same source
/// </summary>
public class PackageCatalog
{
    public string Name
    {
        get; init;
    }

    public string Description
    {
        get; init;
    }

    public IReadOnlyCollection<IWinGetPackage> Packages
    {
        get; init;
    }
}
