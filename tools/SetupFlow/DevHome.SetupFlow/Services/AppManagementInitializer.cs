// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Services.DesiredStateConfiguration.Contracts;
using DevHome.Services.WindowsPackageManager.Contracts;
using Serilog;

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Class responsible for initializing the App Management system in the setup flow.
/// </summary>
public class AppManagementInitializer : IAppManagementInitializer
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(AppManagementInitializer));
    private readonly IWinGet _wpm;
    private readonly ICatalogDataSourceLoader _catalogDataSourceLoader;
    private readonly IDSC _dsc;

    public AppManagementInitializer(
        IWinGet wpm,
        IDSC dsc,
        ICatalogDataSourceLoader catalogDataSourceLoader)
    {
        _wpm = wpm;
        _dsc = dsc;
        _catalogDataSourceLoader = catalogDataSourceLoader;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _log.Information($"Initializing app management");

        // Initialize catalogs from all data sources
        await InitializeCatalogsAsync();

        // Ensure AppInstaller is registered
        if (await TryRegisterAppInstallerAsync())
        {
            await Task.WhenAll(
                UnstubConfigurationAsync(),
                InitializeWindowsPackageManagerAsync());
        }

        _log.Information($"Completed app management initialization");
    }

    /// <inheritdoc />
    public async Task ReinitializeAsync()
    {
        _log.Information($"Reinitializing app management");
        await InitializeWindowsPackageManagerAsync();
        _log.Information($"Completed app management reinitialization");
    }

    /// <summary>
    /// Initialize app management services
    /// </summary>
    private async Task InitializeWindowsPackageManagerAsync()
    {
        try
        {
            _log.Information($"Ensuring app management initialization");

            // Initialize windows package manager after AppInstaller is registered
            await _wpm.InitializeAsync();

            // Load catalogs from all data sources
            await LoadCatalogsAsync();

            _log.Information($"Finished ensuring app management initialization");
        }
        catch (Exception e)
        {
            _log.Error(e, $"Unable to correctly initialize app management at the moment. Further attempts will be performed later.");
        }
    }

    /// <summary>
    /// Initialize catalogs from all data sources (e.g. Restore packages, etc ...)
    /// </summary>
    private async Task InitializeCatalogsAsync()
    {
        _log.Information("Initialize catalogs from all data sources");
        await _catalogDataSourceLoader.InitializeAsync();
    }

    /// <summary>
    /// Loading catalogs from all data sources(e.g. Restore packages, etc ...)
    /// </summary>
    private async Task LoadCatalogsAsync()
    {
        _log.Information($"Loading catalogs from all data sources at launch time to reduce the wait time when this information is requested");
        await foreach (var dataSourceCatalogs in _catalogDataSourceLoader.LoadCatalogsAsync())
        {
            _log.Information($"Loaded {dataSourceCatalogs.Count} catalogs [{string.Join(", ", dataSourceCatalogs.Select(c => c.Name))}]");
        }
    }

    private async Task UnstubConfigurationAsync()
    {
        var isUnstubbed = await _dsc.IsUnstubbedAsync();
        _log.Information($"Configuration is {(isUnstubbed ? "unstubbed" : "stubbed")}");
        if (!isUnstubbed)
        {
            _log.Information($"Starting to unstub configuration");
            var unstubResult = await _dsc.UnstubAsync();
            _log.Information($"Finished unstubbing configuration with result: {unstubResult}");
        }
    }

    /// <summary>
    /// Try to register AppInstaller
    /// </summary>
    /// <returns>True if AppInstaller is registered, false otherwise</returns>
    private async Task<bool> TryRegisterAppInstallerAsync()
    {
        _log.Information("Ensuring AppInstaller is registered ...");

        // If WinGet COM Server is available, then AppInstaller is registered
        if (await _wpm.IsAvailableAsync())
        {
            _log.Information("AppInstaller is already registered");
            return true;
        }

        _log.Information("WinGet COM Server is not available. AppInstaller might be staged but not registered, attempting to register it to fix the issue");
        if (await _wpm.RegisterAppInstallerAsync())
        {
            if (await _wpm.IsAvailableAsync())
            {
                _log.Information("AppInstaller was registered successfully");
                return true;
            }

            _log.Error("WinGet COM Server is not available after AppInstaller registration");
        }
        else
        {
            _log.Error("AppInstaller was not registered");
        }

        return false;
    }
}
