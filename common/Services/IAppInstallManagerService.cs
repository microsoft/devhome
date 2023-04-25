// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;
using Windows.ApplicationModel.Store.Preview.InstallControl;

namespace DevHome.Services;

public interface IAppInstallManagerService
{
    public Task<bool> IsAppUpdateAvailableAsync(string productId);

    public Task<AppInstallItem> StartAppUpdateAsync(string productId);

    public Task<AppInstallItem> StartAppInstallAsync(string productId, bool repair, bool forceUseOfNonRemovableStorage)
}
