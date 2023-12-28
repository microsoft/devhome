// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

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
}
