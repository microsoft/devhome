// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using DevHome.Services.WindowsPackageManager.Contracts;

namespace DevHome.SetupFlow.Models;

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
