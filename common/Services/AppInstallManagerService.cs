// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Store.Preview.InstallControl;
using Windows.Foundation;

namespace DevHome.Services;

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

    public async Task StartAppInstallAsync(string productId, bool repair, bool forceUseOfNonRemovableStorage)
    {
        await _appInstallManager.StartAppInstallAsync(
            productId,
            skuId: null,
            repair,
            forceUseOfNonRemovableStorage);
    }

    /// <summary>
    /// Search for an update for the specified product id
    /// </summary>
    /// <param name="productId">Target product id</param>
    /// <param name="options">Update option</param>
    /// <returns>True if an update is available</returns>
    private async Task<bool> SearchForUpdateAsync(string productId, AppUpdateOptions options)
    {
        // Throws an exception if product id is invalid
        var appInstallItem = await _appInstallManager.SearchForUpdatesAsync(
            productId,
            skuId: null,
            correlationVector: null,
            clientId: null,
            options);

        // Check if update is available
        return appInstallItem != null;
    }
}
