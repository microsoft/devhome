// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;
using DevHome.SetupFlow.Models;
using WPMPackageCatalog = Microsoft.Management.Deployment.PackageCatalog;

namespace DevHome.SetupFlow.Services.WinGet;
public interface IWinGetPackageInstaller
{
    public Task<InstallPackageResult> InstallPackageAsync(WPMPackageCatalog catalog, string packageId);
}
