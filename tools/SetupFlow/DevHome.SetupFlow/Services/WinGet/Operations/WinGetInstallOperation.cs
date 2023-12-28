// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet.Operations;

/// <summary>
/// Install packages using WinGet with recovery
/// </summary>
internal class WinGetInstallOperation : IWinGetInstallOperation
{
    private readonly IWinGetPackageInstaller _packageInstaller;
    private readonly IWinGetProtocolParser _protocolParser;
    private readonly IWinGetRecovery _recovery;

    public WinGetInstallOperation(
        IWinGetPackageInstaller packageInstaller,
        IWinGetProtocolParser protocolParser,
        IWinGetRecovery recovery)
    {
        _packageInstaller = packageInstaller;
        _protocolParser = protocolParser;
        _recovery = recovery;
    }

    /// <inheritdoc />
    public async Task<InstallPackageResult> InstallPackageAsync(IWinGetPackage package)
    {
        return await InstallPackageAsync(_protocolParser.CreatePackageUri(package));
    }

    /// <summary>
    /// Installs a package from a URI.
    /// </summary>
    /// <param name="packageUri">Uri of the package to install.</param>
    /// <returns>Result of the installation.</returns>
    private async Task<InstallPackageResult> InstallPackageAsync(Uri packageUri)
    {
        var parsedPackageUri = _protocolParser.ParsePackageUri(packageUri);
        if (parsedPackageUri == null)
        {
            throw new ArgumentException($"Invalid package URI ${packageUri}");
        }

        return await _recovery.DoWithRecoveryAsync(async () =>
        {
            var catalog = await _protocolParser.ResolveCatalogAsync(parsedPackageUri);
            return await _packageInstaller.InstallPackageAsync(catalog, parsedPackageUri.packageId);
        });
    }
}
