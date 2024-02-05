// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet.Operations;

internal sealed class WinGetOperations : IWinGetOperations
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

    /// <inheritdoc />
    public async Task<InstallPackageResult> InstallPackageAsync(IWinGetPackage package) => await _installOperation.InstallPackageAsync(package);

    /// <inheritdoc />
    public async Task<IList<IWinGetPackage>> GetPackagesAsync(IList<Uri> packageUris) => await _getPackageOperation.GetPackagesAsync(packageUris);

    /// <inheritdoc />
    public async Task<IList<IWinGetPackage>> SearchAsync(string query, uint limit) => await _searchOperation.SearchAsync(query, limit);
}
