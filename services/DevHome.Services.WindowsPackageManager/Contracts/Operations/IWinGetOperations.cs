﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Services.WindowsPackageManager.Models;

namespace DevHome.Services.WindowsPackageManager.Contracts.Operations;

internal interface IWinGetOperations
{
    /// <inheritdoc cref="IWinGetInstallOperation.InstallPackageAsync"/>"
    public Task<InstallPackageResult> InstallPackageAsync(WinGetPackageUri packageUri);

    /// <inheritdoc cref="IWinGetGetPackageOperation.GetPackagesAsync"/>"
    public Task<IList<IWinGetPackage>> GetPackagesAsync(IList<WinGetPackageUri> packageUris);

    /// <inheritdoc cref="IWinGetSearchOperation.SearchAsync"/>"
    public Task<IList<IWinGetPackage>> SearchAsync(string query, uint limit);
}
