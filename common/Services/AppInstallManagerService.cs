// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Store.Preview.InstallControl;

namespace DevHome.Services;

public class AppInstallManagerService : IAppInstallManagerService
{
    private readonly AppInstallManager _appInstallManager;

    public AppInstallManagerService()
    {
        _appInstallManager = new AppInstallManager();
    }

    public async Task<bool> IsAppUpdateAvailableAsync(string productId)
    {
        var appInstallItem = await SearchForUpdateAsync(productId, new AppUpdateOptions
        {
            AutomaticallyDownloadAndInstallUpdateIfFound = false,
        });

        return appInstallItem != null;
    }

    public async Task<AppInstallItem> StartAppUpdateAsync(string productId)
    {
        return await SearchForUpdateAsync(productId, new AppUpdateOptions
        {
            AutomaticallyDownloadAndInstallUpdateIfFound = true,
        });
    }

    public async Task<AppInstallItem> StartAppInstallAsync(string productId, bool repair, bool forceUseOfNonRemovableStorage)
    {
        return await _appInstallManager.StartAppInstallAsync(
            productId,
            skuId: null,
            repair,
            forceUseOfNonRemovableStorage);
    }

    private async Task<AppInstallItem> SearchForUpdateAsync(string productId, AppUpdateOptions options)
    {
        return await _appInstallManager.SearchForUpdatesAsync(
            productId,
            skuId: null,
            correlationVector: null,
            clientId: null,
            options);
    }
}
