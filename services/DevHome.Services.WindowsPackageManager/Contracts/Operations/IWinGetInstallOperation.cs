// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using DevHome.Services.WindowsPackageManager.Models;

namespace DevHome.Services.WindowsPackageManager.Contracts.Operations;

internal interface IWinGetInstallOperation
{
    /// <summary>
    /// Installs a package from a URI.
    /// </summary>
    /// <param name="packageUri">Uri of the package to install.</param>
    /// <returns>Result of the installation.</returns>
    public Task<InstallPackageResult> InstallPackageAsync(WinGetPackageUri packageUri);
}
