// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;
using Windows.ApplicationModel.Store.Preview.InstallControl;
using Windows.Foundation;

namespace DevHome.Services;

public interface IAppInstallManagerService
{
    public event TypedEventHandler<AppInstallManager, AppInstallManagerItemEventArgs> ItemCompleted;

    public event TypedEventHandler<AppInstallManager, AppInstallManagerItemEventArgs> ItemStatusChanged;

    public Task<bool> IsAppUpdateAvailableAsync(string productId);

    public Task<bool> StartAppUpdateAsync(string productId);

    public Task StartAppInstallAsync(string productId, bool repair, bool forceUseOfNonRemovableStorage);
}
