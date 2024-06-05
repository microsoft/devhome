// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Services.WindowsPackageManager.Contracts;
using DevHome.Services.WindowsPackageManager.Contracts.Operations;
using DevHome.Services.WindowsPackageManager.Models;

namespace DevHome.Services.WindowsPackageManager.Services;

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
    public async Task<WinGetInstallPackageResult> InstallPackageAsync(WinGetPackageUri packageUri) => await _installOperation.InstallPackageAsync(packageUri);

    /// <inheritdoc />
    public async Task<IList<IWinGetPackage>> GetPackagesAsync(IList<WinGetPackageUri> packageUris) => await _getPackageOperation.GetPackagesAsync(packageUris);

    /// <inheritdoc />
    public async Task<IList<IWinGetPackage>> SearchAsync(string query, uint limit) => await _searchOperation.SearchAsync(query, limit);
}
