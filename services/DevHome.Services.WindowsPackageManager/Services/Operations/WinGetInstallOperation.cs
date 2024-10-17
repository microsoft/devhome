// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices.WindowsRuntime;
using DevHome.Services.WindowsPackageManager.Contracts;
using DevHome.Services.WindowsPackageManager.Models;
using Windows.Foundation;

namespace DevHome.Services.WindowsPackageManager.Services.Operations;

/// <summary>
/// Install packages using WinGet with recovery
/// </summary>
internal sealed class WinGetInstallOperation : IWinGetInstallOperation
{
    private readonly IWinGetPackageInstaller _packageInstaller;
    private readonly IWinGetProtocolParser _protocolParser;
    private readonly IWinGetCatalogConnector _catalogConnector;
    private readonly IWinGetRecovery _recovery;

    public WinGetInstallOperation(
        IWinGetPackageInstaller packageInstaller,
        IWinGetProtocolParser protocolParser,
        IWinGetCatalogConnector catalogConnector,
        IWinGetRecovery recovery)
    {
        _packageInstaller = packageInstaller;
        _protocolParser = protocolParser;
        _catalogConnector = catalogConnector;
        _recovery = recovery;
    }

    /// <inheritdoc />
    public IAsyncOperationWithProgress<IWinGetInstallPackageResult, WinGetInstallPackageProgress> InstallPackageAsync(WinGetPackageUri packageUri, Guid activityId)
    {
        return AsyncInfo.Run<IWinGetInstallPackageResult, WinGetInstallPackageProgress>(async (_, progress) =>
        {
            // If recovery was initiated due to RPC failure, we need to
            // re-attempt the operation and restart the progress reporting.
            return await _recovery.DoWithRecoveryAsync(async () =>
            {
                var catalog = await _protocolParser.ResolveCatalogAsync(packageUri);
                var install = _packageInstaller.InstallPackageAsync(catalog, packageUri.PackageId, packageUri.Options.Version, activityId);
                install.Progress += (_, p) => progress.Report(p);
                return await install;
            });
        });
    }
}
