// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet.Operations;

public class WinGetInstallOperation : IWinGetInstallOperation
{
    private readonly IWinGetCatalogConnector _catalogConnector;
    private readonly IWinGetPackageInstaller _packageInstaller;
    private readonly IWinGetRecovery _recovery;

    public WinGetInstallOperation(
        IWinGetCatalogConnector catalogConnector,
        IWinGetPackageInstaller packageInstaller,
        IWinGetRecovery recovery)
    {
        _catalogConnector = catalogConnector;
        _packageInstaller = packageInstaller;
        _recovery = recovery;
    }

    public async Task<InstallPackageResult> InstallPackageAsync(IWinGetPackage package)
    {
        return await _recovery.DoWithRecovery(async () =>
        {
            var catalog = await _catalogConnector.GetPackageCatalogAsync(package);
            return await _packageInstaller.InstallPackageAsync(catalog, package.Id);
        });
    }
}
