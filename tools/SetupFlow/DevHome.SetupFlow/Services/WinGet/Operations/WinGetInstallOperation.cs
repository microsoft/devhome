// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
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
    public async Task<InstallPackageResult> InstallPackageAsync(IWinGetPackage package)
    {
        return await _recovery.DoWithRecoveryAsync(async () =>
        {
            var catalog = await _catalogConnector.GetPackageCatalogAsync(package);
            return await _packageInstaller.InstallPackageAsync(catalog, package.Id);
        });
    }

    /// <inheritdoc />
    public async Task<InstallPackageResult> InstallPackageAsync(Uri packageUri)
    {
        var parsedPackageUri = _protocolParser.ParsePackageUri(packageUri);
        if (parsedPackageUri == null)
        {
            throw new ArgumentException($"Invalid package URI ${packageUri}");
        }

        return await _recovery.DoWithRecoveryAsync(async () =>
        {
            var catalog = await _protocolParser.ResolveCatalogAsync(parsedPackageUri);
            return await _packageInstaller.InstallPackageAsync(catalog, parsedPackageUri.PackageId);
        });
    }
}
