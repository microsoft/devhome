// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Store.Preview.InstallControl;
using Windows.Foundation;

namespace DevHome.Services.Core.Contracts;

public interface IMicrosoftStoreService
{
    /// <inheritdoc cref="AppInstallManager.ItemCompleted"/>
    public event TypedEventHandler<AppInstallManager, AppInstallManagerItemEventArgs> ItemCompleted;

    /// <inheritdoc cref="AppInstallManager.ItemStatusChanged"/>
    public event TypedEventHandler<AppInstallManager, AppInstallManagerItemEventArgs> ItemStatusChanged;

    /// <summary>
    /// Checks if an application has an update without performing the update
    /// </summary>
    /// <param name="productId">Target product id</param>
    /// <returns>True if an app update is available, false otherwise.</returns>
    /// <exception cref="COMException">Throws exception if operation failed (e.g. product id was not found)</exception>
    public Task<bool> IsAppUpdateAvailableAsync(string productId);

    /// <summary>
    /// Start updating an application if one is available.
    /// </summary>
    /// <param name="productId">Target product id</param>
    /// <returns>True if an app update was triggered, false otherwise</returns>
    /// <exception cref="COMException">Throws exception if operation failed (e.g. product id was not found)</exception>
    public Task<bool> StartAppUpdateAsync(string productId);

    /// <summary>
    /// Install a package from the store.
    /// </summary>
    /// <param name="packageId">Target package id</param>
    /// <returns>True if package was installed, false otherwise</returns>
    public Task<bool> TryInstallPackageAsync(string packageId);

    /// <summary>
    /// Install a package from the store.
    /// </summary>
    /// <param name="packageId">Target package id</param>
    /// <param name="timeout">Amount of time the operation should wait before cancelling the task.</param>
    public Task InstallPackageAsync(string packageId, TimeSpan timeout);
}
