// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Services.WindowsPackageManager.Models;

namespace DevHome.Services.WindowsPackageManager.Contracts;

/// <summary>
/// Interface for interacting with the WinGet package manager.
/// More details: https://github.com/microsoft/winget-cli/blob/master/src/Microsoft.Management.Deployment/PackageManager.idl
/// </summary>
public interface IWinGet
{
    /// <summary>
    /// Initialize the winget package manager.
    /// </summary>
    public Task InitializeAsync();

    /// <inheritdoc cref="IWinGetOperations.InstallPackageAsync"/>
    public Task<InstallPackageResult> InstallPackageAsync(WinGetPackageUri packageUri);

    /// <inheritdoc cref="IWinGetOperations.GetPackagesAsync"/>
    public Task<IList<IWinGetPackage>> GetPackagesAsync(IList<WinGetPackageUri> packageUris);

    /// <inheritdoc cref="IWinGetOperations.SearchAsync"/>
    public Task<IList<IWinGetPackage>> SearchAsync(string query, uint limit);

    /// <inheritdoc cref="IWinGetDeployment.IsUpdateAvailableAsync"/>
    public Task<bool> IsUpdateAvailableAsync();

    /// <inheritdoc cref="IWinGetDeployment.IsAvailableAsync"/>
    public Task<bool> IsAvailableAsync();

    /// <inheritdoc cref="IWinGetDeployment.RegisterAppInstallerAsync"/>
    public Task<bool> RegisterAppInstallerAsync();

    /// <inheritdoc cref="IWinGetCatalogConnector.IsMsStorePackage"/>
    public bool IsMsStorePackage(IWinGetPackage package);

    /// <inheritdoc cref="IWinGetCatalogConnector.IsWinGetPackage"/>
    public bool IsWinGetPackage(IWinGetPackage package);

    /// <inheritdoc cref="IWinGetProtocolParser.CreatePackageUri"/>
    public WinGetPackageUri CreatePackageUri(IWinGetPackage package);

    /// <inheritdoc cref="IWinGetProtocolParser.CreateWinGetCatalogPackageUri"/>
    public WinGetPackageUri CreateWinGetCatalogPackageUri(string packageId);

    /// <inheritdoc cref="IWinGetProtocolParser.CreateMsStoreCatalogPackageUri"/>
    public WinGetPackageUri CreateMsStoreCatalogPackageUri(string packageId);

    /// <inheritdoc cref="IWinGetProtocolParser.CreateCustomCatalogPackageUri"/>
    public WinGetPackageUri CreateCustomCatalogPackageUri(string packageId, string catalogName);
}
