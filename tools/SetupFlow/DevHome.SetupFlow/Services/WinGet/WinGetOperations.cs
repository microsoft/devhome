// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet.Operations;
public class WinGetOperations : IWinGetOperations
{
    private readonly IWinGetInstallOperation _installOperation;
    private readonly IWinGetSearchOperation _searchOperation;
    private readonly IWinGetGetPackageOperation _getPackageOperation;

    public WinGetOperations(
        IWinGetInstallOperation installOperation,
        IWinGetSearchOperation searchOperation,
        IWinGetGetPackageOperation getPackageOperation)
    {
        _installOperation = installOperation;
        _searchOperation = searchOperation;
        _getPackageOperation = getPackageOperation;
    }

    public async Task<InstallPackageResult> InstallPackageAsync(IWinGetPackage package)
    {
        return await _installOperation.InstallPackageAsync(package);
    }

    public async Task<IList<IWinGetPackage>> GetPackagesAsync(ISet<Uri> packageUriSet)
    {
        return await _getPackageOperation.GetPackagesAsync(packageUriSet);
    }

    public async Task<IList<IWinGetPackage>> SearchAsync(string query, uint limit)
    {
        return await _searchOperation.SearchAsync(query, limit);
    }
}
