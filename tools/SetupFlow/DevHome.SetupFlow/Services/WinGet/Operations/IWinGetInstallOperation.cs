// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet.Operations;

internal interface IWinGetInstallOperation
{
    /// <summary>
    /// Install a package on the user's machine.
    /// </summary>
    /// <param name="package">Package to install</param>
    /// <returns>Install package result</returns>
    public Task<InstallPackageResult> InstallPackageAsync(IWinGetPackage package);

    /// <summary>
    /// Installs a package from a URI.
    /// </summary>
    /// <param name="packageUri">Uri of the package to install.</param>
    /// <returns>Result of the installation.</returns>
    public Task<InstallPackageResult> InstallPackageAsync(Uri packageUri);
}
