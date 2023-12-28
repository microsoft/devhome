// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet.Operations;

internal interface IWinGetGetPackageOperation
{
    /// <summary>
    /// Get packages from a set of package uri.
    /// </summary>
    /// <param name="packageUris">Set of package uri</param>
    /// <returns>List of winget package matches</returns>
    /// <exception cref="FindPackagesException">Exception thrown if the get packages operation failed</exception>
    public Task<IList<IWinGetPackage>> GetPackagesAsync(ISet<Uri> packageUris);
}
