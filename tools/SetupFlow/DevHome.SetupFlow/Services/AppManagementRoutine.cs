// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Helpers;

namespace DevHome.SetupFlow.Services;
public class AppManagementRoutine : IAppManagementRoutine
{
    private const int MaxRetry = 5;
    private readonly TimeSpan _retryTimeout = TimeSpan.FromMinutes(2);

    private readonly IWindowsPackageManager _wpm;
    private readonly CatalogDataSourceLoacder _catalogDataSourceLoader;

    public AppManagementRoutine(
        IWindowsPackageManager wpm,
        CatalogDataSourceLoacder catalogDataSourceLoader)
    {
        _wpm = wpm;
        _catalogDataSourceLoader = catalogDataSourceLoader;
    }

    public async Task InitializeAsync()
    {
        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Initializing {nameof(AppManagementRoutine)}");
        await InitializeCatalogsAsync();
        await InstallAppInstallerWithRetryAsync(MaxRetry, _retryTimeout);
        await ConnectWithRertyAsync(MaxRetry, _retryTimeout);
        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Completed {nameof(AppManagementRoutine)} initialization");
    }

    private async Task InitializeCatalogsAsync()
    {
        Log.Logger?.ReportInfo(Log.Component.AppManagement, "Initialize catalogs from all data sources");
        await _catalogDataSourceLoader.InitializeAsync();
    }

    private async Task InstallAppInstallerWithRetryAsync(int maxRetries, TimeSpan timeout)
    {
        if (await _wpm.IsAppInstallerInstalledAsync())
        {
            Log.Logger?.ReportInfo(Log.Component.AppManagement, "AppInstaller is already installed");
        }
        else
        {
            for (var retry = 1; retry <= maxRetries; ++retry)
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Enqueuing AppInstaller installation. Attempt: {retry} / {maxRetries}");

                // Queue installation
                if (await _wpm.StartAppInstallerInstallAsync())
                {
                    Log.Logger?.ReportInfo(Log.Component.AppManagement, $"AppInstaller installation enqueued. Will wait {timeout} to verify the installation");
                }
                else
                {
                    Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Failed to enqueue AppInstaller installation. Will wait {timeout} before retrying");
                }

                // Wait for the installation
                await Task.Delay(timeout);

                if (await _wpm.IsAppInstallerInstalledAsync())
                {
                    Log.Logger?.ReportInfo(Log.Component.AppManagement, "AppInstaller was installed successfully");
                    return;
                }

                Log.Logger?.ReportInfo(Log.Component.AppManagement, "AppInstaller was not detected. Will retry again if more attempts are available");
            }
        }
    }

    public async Task ConnectWithRertyAsync(int maxRetries, TimeSpan timeout)
    {
        for (var retry = 1; retry <= maxRetries; ++retry)
        {
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Checking if WinGet COM Server is available. Attempt: {retry} / {maxRetries}");
            if (await _wpm.IsCOMServerAvailableAsync())
            {
                await _wpm.ConnectToAllCatalogsAsync();
                return;
            }

            if (retry < maxRetries)
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Will wait {timeout} before checking again if WinGet COM Server is available");
                await Task.Delay(timeout);
            }
            else
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"No more attempts available for checking if WinGet COM Server is available");
            }
        }
    }
}
