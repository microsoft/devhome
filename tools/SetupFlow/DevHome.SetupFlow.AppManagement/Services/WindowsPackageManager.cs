// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.AppManagement.Exceptions;
using DevHome.SetupFlow.AppManagement.Models;
using DevHome.SetupFlow.ComInterop.Projection.WindowsPackageManager;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.Telemetry;
using Microsoft.Management.Deployment;

namespace DevHome.SetupFlow.AppManagement.Services;

/// <summary>
/// Windows package manager class is an entrypoint for using the WinGet COM API.
/// </summary>
public class WindowsPackageManager : IWindowsPackageManager
{
    private readonly WindowsPackageManagerFactory _wingetFactory;

    // Custom composite catalogs
    private readonly Lazy<WinGetCompositeCatalog> _allCatalogs;
    private readonly Lazy<WinGetCompositeCatalog> _wingetCatalog;
    private readonly Lazy<WinGetCompositeCatalog> _msStoreCatalog;

    // Predefined catalog ids
    private readonly Lazy<string> _wingetCatalogId;
    private readonly Lazy<string> _msStoreCatalogId;

    public WindowsPackageManager(WindowsPackageManagerFactory wingetFactory)
    {
        _wingetFactory = wingetFactory;

        // Lazy-initialize custom composite catalogs
        _allCatalogs = new (CreateAllCatalogs);
        _wingetCatalog = new (CreateWinGetCatalog);
        _msStoreCatalog = new (CreateMsStoreCatalog);

        // Lazy-initialize predefined catalog ids
        _wingetCatalogId = new (() => GetPredefinedCatalogId(PredefinedPackageCatalog.OpenWindowsCatalog));
        _msStoreCatalogId = new (() => GetPredefinedCatalogId(PredefinedPackageCatalog.MicrosoftStore));
    }

    public string WinGetCatalogId => _wingetCatalogId.Value;

    public string MsStoreId => _msStoreCatalogId.Value;

    public IWinGetCatalog AllCatalogs => _allCatalogs.Value;

    public IWinGetCatalog WinGetCatalog => _wingetCatalog.Value;

    public IWinGetCatalog MsStoreCatalog => _msStoreCatalog.Value;

    public async Task ConnectToAllCatalogsAsync()
    {
        Log.Logger?.ReportInfo(nameof(WindowsPackageManager), "Connecting to all catalogs");

        // Connect composite catalog for all local and remote catalogs to
        // enable searching for pacakges from any source
        await AllCatalogs.ConnectAsync();

        // Connect predefined winget catalog to enable loading
        // package with a known source (e.g. for restoring packages)
        await WinGetCatalog.ConnectAsync();
    }

    public async Task<InstallPackageResult> InstallPackageAsync(WinGetPackage package)
    {
        var packageManager = _wingetFactory.CreatePackageManager();
        var options = _wingetFactory.CreateInstallOptions();
        options.PackageInstallMode = PackageInstallMode.Silent;

        Log.Logger?.ReportInfo(nameof(WindowsPackageManager), $"Starting package install for {package.Id}");
        var installResult = await packageManager.InstallPackageAsync(package.CatalogPackage, options).AsTask();

        Log.Logger?.ReportInfo(
            nameof(WindowsPackageManager),
            $"Install result: Status={installResult.Status}, InstallerErrorCode={installResult.InstallerErrorCode}, RebootRequired={installResult.RebootRequired}");

        if (installResult.Status != InstallResultStatus.Ok)
        {
            throw new InstallPackageException(installResult.Status, installResult.InstallerErrorCode);
        }

        return new ()
        {
            RebootRequired = installResult.RebootRequired,
        };
    }

    /// <summary>
    /// Gets the id of the provided predefined catalog
    /// </summary>
    /// <param name="catalog">Predefined catalog</param>
    /// <returns>Catalog id</returns>
    private string GetPredefinedCatalogId(PredefinedPackageCatalog catalog)
    {
        var packageManager = _wingetFactory.CreatePackageManager();
        var packageCatalog = packageManager.GetPredefinedPackageCatalog(catalog);
        return packageCatalog.Info.Id;
    }

    /// <summary>
    /// Create a composite catalog that can be used for finding packages in all
    /// remote and local packages
    /// </summary>
    /// <returns>Catalog composed of all remote and local catalogs</returns>
    private WinGetCompositeCatalog CreateAllCatalogs()
    {
        var compositeCatalog = new WinGetCompositeCatalog(_wingetFactory);
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
        var compositeCatalog = new WinGetCompositeCatalog(_wingetFactory);
        compositeCatalog.CompositeSearchBehavior = CompositeSearchBehavior.RemotePackagesFromAllCatalogs;
        var packageManager = _wingetFactory.CreatePackageManager();
        var catalog = packageManager.GetPredefinedPackageCatalog(predefinedPackageCatalog);
        compositeCatalog.AddPackageCatalog(catalog);
        return compositeCatalog;
    }
}
