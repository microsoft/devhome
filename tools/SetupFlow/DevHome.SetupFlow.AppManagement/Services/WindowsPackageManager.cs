// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.AppManagement.Models;
using DevHome.SetupFlow.ComInterop.Projection.WindowsPackageManager;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Management.Deployment;

namespace DevHome.SetupFlow.AppManagement.Services;

/// <summary>
/// Windows package manager class is an entrypoint for using the WinGet COM API.
/// </summary>
public class WindowsPackageManager : IWindowsPackageManager
{
    private readonly ILogger _logger;
    private readonly IHost _host;
    private readonly WindowsPackageManagerFactory _wingetFactory;

    // Predefined catalogs
    private readonly Lazy<WinGetCompositeCatalog> _allCatalogs;
    private readonly Lazy<WinGetCompositeCatalog> _wingetCatalog;
    private readonly Lazy<WinGetCompositeCatalog> _msStoreCatalog;

    public WindowsPackageManager(IHost host, ILogger logger, WindowsPackageManagerFactory wingetFactory)
    {
        _host = host;
        _logger = logger;
        _wingetFactory = wingetFactory;

        // Lazy-initialize catalogs
        _allCatalogs = new (CreateAllCatalogs);
        _wingetCatalog = new (CreateWinGetCatalog);
        _msStoreCatalog = new (CreateMsStoreCatalog);
    }

    public IWinGetCatalog AllCatalogs => _allCatalogs.Value;

    public IWinGetCatalog WinGetCatalog => _wingetCatalog.Value;

    public IWinGetCatalog MsStoreCatalog => _msStoreCatalog.Value;

    public async Task InstallPackageAsync(WinGetPackage package)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    /// <summary>
    /// Create a composite catalog that can be used for finding packages in all
    /// remote and local packages
    /// </summary>
    /// <returns>Catalog composed of all remote and local catalogs</returns>
    private WinGetCompositeCatalog CreateAllCatalogs()
    {
        var compositeCatalog = new WinGetCompositeCatalog(_logger, _wingetFactory);
        compositeCatalog.CompositeSearchBehavior = CompositeSearchBehavior.RemotePackagesFromAllCatalogs;
        var packageManager = _wingetFactory.CreatePackageManager();
        var catalogs = packageManager.GetPackageCatalogs();

        // Cannot use foreach or Linq for out-of-process IVector
        // Bug: https://github.com/microsoft/CsWinRT/issues/1205
        for (var i = 0; i < catalogs.Count; ++i)
        {
            compositeCatalog.AddPackageCatalog(catalogs[i]);
        }

        return compositeCatalog;
    }

    /// <summary>
    /// Create a composite catalog that can be used for finding packages in
    /// winget and local catalogs
    /// </summary>
    /// <returns>Catalog composed of winget and local catalogs</returns>
    private WinGetCompositeCatalog CreateWinGetCatalog()
    {
        return CreatePredefinedCatalog(PredefinedPackageCatalog.OpenWindowsCatalog);
    }

    /// <summary>
    /// Create a composite catalog that can be used for finding packages in
    /// msstore and local catalogs
    /// </summary>
    /// <returns>Catalog composed of msstore and local catalogs</returns>
    private WinGetCompositeCatalog CreateMsStoreCatalog()
    {
        return CreatePredefinedCatalog(PredefinedPackageCatalog.MicrosoftStore);
    }

    /// <summary>
    /// Create a composite catalog that can be used for finding packages in
    /// a predefined and local catalogs
    /// </summary>
    /// <param name="predefinedPackageCatalog">Predefined package catalog</param>
    /// <returns>Catalog composed of the provided and local catalogs</returns>
    private WinGetCompositeCatalog CreatePredefinedCatalog(PredefinedPackageCatalog predefinedPackageCatalog)
    {
        var compositeCatalog = new WinGetCompositeCatalog(_logger, _wingetFactory);
        compositeCatalog.CompositeSearchBehavior = CompositeSearchBehavior.RemotePackagesFromAllCatalogs;
        var packageManager = _wingetFactory.CreatePackageManager();
        var catalog = packageManager.GetPredefinedPackageCatalog(predefinedPackageCatalog);
        compositeCatalog.AddPackageCatalog(catalog);
        return compositeCatalog;
    }
}
