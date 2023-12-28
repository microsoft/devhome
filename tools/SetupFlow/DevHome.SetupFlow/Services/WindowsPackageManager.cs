// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services.WinGet;
using DevHome.SetupFlow.Services.WinGet.Operations;

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Windows package manager class is an entry point for using the WinGet COM API.
/// </summary>
internal class WindowsPackageManager : IWindowsPackageManager
{
    // WinGet services
    private readonly IWinGetCatalogConnector _catalogConnector;
    private readonly IWinGetDeployment _deployment;
    private readonly IWinGetOperations _operations;
    private readonly IWinGetProtocolParser _protocolParser;

    public static string AppInstallerProductId => WinGetDeployment.AppInstallerProductId;

    public static int AppInstallerErrorFacility => WinGetDeployment.AppInstallerErrorFacility;

    public WindowsPackageManager(
        IWinGetCatalogConnector catalogConnector,
        IWinGetDeployment deployment,
        IWinGetOperations operations,
        IWinGetProtocolParser protocolParser)
    {
        _catalogConnector = catalogConnector;
        _deployment = deployment;
        _operations = operations;
        _protocolParser = protocolParser;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        // Run action in a background thread to avoid blocking the UI thread
        // Async methods are blocking in WinGet: https://github.com/microsoft/winget-cli/issues/3205
        await Task.Run(async () => await _catalogConnector.CreateAndConnectCatalogsAsync());
    }

    /// <inheritdoc/>
    public async Task<InstallPackageResult> InstallPackageAsync(IWinGetPackage package) => await _operations.InstallPackageAsync(package);

    /// <inheritdoc/>
    public async Task<IList<IWinGetPackage>> GetPackagesAsync(ISet<Uri> packageUris) => await _operations.GetPackagesAsync(packageUris);

    /// <inheritdoc/>
    public async Task<IList<IWinGetPackage>> SearchAsync(string query, uint limit) => await _operations.SearchAsync(query, limit);

    /// <inheritdoc/>
    public async Task<bool> IsUpdateAvailableAsync() => await _deployment.IsUpdateAvailableAsync();

    /// <inheritdoc/>
    public async Task<bool> RegisterAppInstallerAsync() => await _deployment.RegisterAppInstallerAsync();

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync() => await _deployment.IsAvailableAsync();

    /// <inheritdoc/>
    public bool IsMsStorePackage(IWinGetPackage package) => _catalogConnector.IsMsStorePackage(package);

    /// <inheritdoc/>
    public bool IsWinGetPackage(IWinGetPackage package) => _catalogConnector.IsWinGetPackage(package);
}
