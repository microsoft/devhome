// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Services.WindowsPackageManager.Models;

namespace DevHome.Services.WindowsPackageManager.Contracts.Operations;

internal interface IWinGetGetPackageOperation
{
    /// <summary>
    /// Get packages from a list of package uri.
    /// </summary>
    /// <param name="packageUris">List of package uri</param>
    /// <returns>List of winget package matches</returns>
    public Task<IList<IWinGetPackage>> GetPackagesAsync(IList<WinGetPackageUri> packageUris);
}
