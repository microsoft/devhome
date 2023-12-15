// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Helpers;

namespace DevHome.SetupFlow.Services;
public class AppManagementInitializer : IAppManagementInitializer
{
    private readonly IWindowsPackageManager _wpm;
    private readonly CatalogDataSourceLoader _catalogDataSourceLoader;

    public AppManagementInitializer(
        IWindowsPackageManager wpm,
        CatalogDataSourceLoader catalogDataSourceLoader)
    {
        _wpm = wpm;
        _catalogDataSourceLoader = catalogDataSourceLoader;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Initializing {nameof(AppManagementInitializer)}");

        // Initialize catalogs from all data sources
        await InitializeCatalogsAsync();

        // Ensure AppInstaller is registered
        if (await TryRegisterAppInstallerAsync())
        {
            await EnsureAppManagementInitializationAsync();
        }

        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Completed {nameof(AppManagementInitializer)} initialization");
    }

    public async Task EnsureAppManagementInitializationAsync()
    {
        try
        {
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Ensuring app management initialization");

            // Initialize windows package manager after AppInstaller is registered
            await _wpm.InitializeAsync();

            // Load catalogs from all data sources
            await LoadCatalogsAsync();

            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Finished ensuring app management initialization");
        }
        catch (Exception e)
        {
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Unable to correctly initialize app management at the moment. Further attempts will be performed later.", e);
        }
    }

    public async Task RefreshAsync()
    {
        try
        {
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Reinitializing app management");
            await _wpm.InitializeAsync();
            await ReloadCatalogsAsync();
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Finished reinitializing app management");
        }
        catch (Exception e)
        {
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Unable to correctly reinitialize app management at the moment. Further attempts will be performed later.", e);
        }
    }

    /// <summary>
    /// Initialize catalogs from all data sources (e.g. Restore packages, etc ...)
    /// </summary>
    private async Task InitializeCatalogsAsync()
    {
        Log.Logger?.ReportInfo(Log.Component.AppManagement, "Initialize catalogs from all data sources");
        await _catalogDataSourceLoader.InitializeAsync();
    }

    /// <summary>
    /// Loading catalogs from all data sources(e.g. Restore packages, etc ...)
    /// </summary>
    private async Task LoadCatalogsAsync()
    {
        Log.Logger?.ReportInfo($"Loading catalogs from all data sources at launch time to reduce the wait time when this information is requested");
        await foreach (var dataSourceCatalogs in _catalogDataSourceLoader.LoadCatalogsAsync())
        {
            Log.Logger?.ReportInfo($"Loaded {dataSourceCatalogs.Count} catalog(s)");
        }
    }

    /// <summary>
    /// Loading catalogs from all data sources(e.g. Restore packages, etc ...)
    /// </summary>
    private async Task ReloadCatalogsAsync()
    {
        Log.Logger?.ReportInfo($"Reloading catalogs from all data sources");
        await foreach (var dataSourceCatalogs in _catalogDataSourceLoader.ReloadCatalogsAsync())
        {
            Log.Logger?.ReportInfo($"Reloaded {dataSourceCatalogs.Count} catalog(s)");
        }
    }

    /// <summary>
    /// Try to register AppInstaller
    /// </summary>
    /// <returns>True if AppInstaller is registered, false otherwise</returns>
    private async Task<bool> TryRegisterAppInstallerAsync()
    {
        Log.Logger?.ReportInfo(Log.Component.AppManagement, "Ensuring AppInstaller is registered ...");

        // If WinGet COM Server is available, then AppInstaller is registered
        if (await _wpm.IsAvailableAsync())
        {
            return true;
        }

        Log.Logger?.ReportInfo(Log.Component.AppManagement, "WinGet COM Server is not available. AppInstaller might be staged but not registered, attempting to register it to fix the issue");
        if (await _wpm.RegisterAppInstallerAsync())
        {
            if (await _wpm.IsAvailableAsync())
            {
                return true;
            }

            Log.Logger?.ReportError(Log.Component.AppManagement, "WinGet COM Server is not available after AppInstaller registration");
        }
        else
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, "AppInstaller was not registered");
        }

        return false;
    }
}
