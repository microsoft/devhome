// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Store.Preview.InstallControl;
using Windows.Foundation;

namespace DevHome.Services;

/// <summary>
/// Service class for using the Store API <see cref="AppInstallManager"/>
/// </summary>
public class AppInstallManagerService : IAppInstallManagerService
{
    private readonly AppInstallManager _appInstallManager;

    public event TypedEventHandler<AppInstallManager, AppInstallManagerItemEventArgs> ItemCompleted
    {
        add => _appInstallManager.ItemCompleted += value;
        remove => _appInstallManager.ItemCompleted -= value;
    }

    public event TypedEventHandler<AppInstallManager, AppInstallManagerItemEventArgs> ItemStatusChanged
    {
        add => _appInstallManager.ItemStatusChanged += value;
        remove => _appInstallManager.ItemStatusChanged -= value;
    }

    public AppInstallManagerService()
    {
        _appInstallManager = new AppInstallManager();
    }

    public async Task<bool> IsAppUpdateAvailableAsync(string productId)
    {
        return await SearchForUpdateAsync(productId, new AppUpdateOptions
        {
            AutomaticallyDownloadAndInstallUpdateIfFound = false,
        });
    }

    public async Task<bool> StartAppUpdateAsync(string productId)
    {
        return await SearchForUpdateAsync(productId, new AppUpdateOptions
        {
            AutomaticallyDownloadAndInstallUpdateIfFound = true,
        });
    }

    public async Task<bool> StartAppInstallAsync(string productId)
    {
        return await InstallAsync(productId, new AppInstallOptions
        {
            CompletedInstallToastNotificationMode = AppInstallationToastNotificationMode.Toast,
            InstallInProgressToastNotificationMode = AppInstallationToastNotificationMode.NoToast,
            ForceUseOfNonRemovableStorage = false,
            Repair = false,
        });
    }

    /// <summary>
    /// Search for an update for the specified product id
    /// </summary>
    /// <param name="productId">Target product id</param>
    /// <param name="options">Update option</param>
    /// <returns>True if an update is available, false otherwise.</returns>
    /// <exception cref="COMException">Throws exception if operation failed (e.g. product id was not found)</exception>
    private async Task<bool> SearchForUpdateAsync(string productId, AppUpdateOptions options)
    {
        var appInstallItem = await _appInstallManager.SearchForUpdatesAsync(
            productId,
            skuId: null,
            correlationVector: null,
            clientId: null,
            options);

        // Check if update is available
        return appInstallItem != null;
    }

    /// <summary>
    /// Start installing an app by product id
    /// </summary>
    /// <param name="productId">Target product id</param>
    /// <param name="options">Install option</param>
    /// <returns>True if an installation was queued, false otherwise.</returns>
    /// <exception cref="COMException">Throws exception if operation failed (e.g. product id was not found)</exception>
    private async Task<bool> InstallAsync(string productId, AppInstallOptions options)
    {
        var appInstallItem = await _appInstallManager.StartProductInstallAsync(
            productId,
            flightId: null,
            clientId: null,
            correlationVector: null,
            options);

        // Check if install was queued
        return appInstallItem != null;
    }
}
