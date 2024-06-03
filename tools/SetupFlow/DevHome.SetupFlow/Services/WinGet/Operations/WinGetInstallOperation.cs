// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet.Operations;

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
    public async Task<InstallPackageResult> InstallPackageAsync(WinGetPackageUri packageUri)
    {
        return await _recovery.DoWithRecoveryAsync(async () =>
        {
            var catalog = await _protocolParser.ResolveCatalogAsync(packageUri);
            return await _packageInstaller.InstallPackageAsync(catalog, packageUri.PackageId, packageUri.Options.Version);
        });
    }
}
