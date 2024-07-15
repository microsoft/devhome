// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DevHome.Services.Core.Contracts;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.Store.Preview.InstallControl;
using Windows.Foundation;

namespace DevHome.Services.Core.Services;

/// <summary>
/// Service class for using the Microsoft Store API
/// https://learn.microsoft.com/uwp/api/windows.applicationmodel.store.preview?view=winrt-22621
/// </summary>
public class MicrosoftStoreService : IMicrosoftStoreService
{
    private readonly AppInstallManager _appInstallManager;
    private readonly TimeSpan _storeInstallTimeout = TimeSpan.FromMinutes(1);
    private readonly ILogger _logger;

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

    public MicrosoftStoreService(ILogger<MicrosoftStoreService> logger)
    {
        _logger = logger;
        _appInstallManager = new();
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

    public async Task<bool> TryInstallPackageAsync(string packageId)
    {
        try
        {
            var installTask = InstallPackageAsync(packageId);

            // Wait for a maximum of StoreInstallTimeout (60 seconds).
            var completedTask = await Task.WhenAny(installTask, Task.Delay(_storeInstallTimeout));

            if (completedTask.Exception != null)
            {
                throw completedTask.Exception;
            }

            if (completedTask != installTask)
            {
                throw new TimeoutException("Store Install task did not finish in time.");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Package installation failed");
        }

        return false;
    }

    private async Task InstallPackageAsync(string packageId)
    {
        await Task.Run(() =>
        {
            var tcs = new TaskCompletionSource<bool>();
            AppInstallItem installItem;
            try
            {
                _logger.LogInformation($"Starting {packageId} install");
                installItem = _appInstallManager.StartAppInstallAsync(packageId, null, true, false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{packageId} install failure");
                tcs.SetException(ex);
                return tcs.Task;
            }

            installItem.Completed += (sender, args) =>
            {
                if (!tcs.TrySetResult(true))
                {
                    _logger.LogInformation($"{packageId} In Completed handler, RanToCompleted already set.");
                }
                else
                {
                    _logger.LogInformation($"{packageId} In Completed handler, RanToCompleted set.");
                }
            };

            installItem.StatusChanged += (sender, args) =>
            {
                if (installItem.GetCurrentStatus().InstallState == AppInstallState.Canceled
                    || installItem.GetCurrentStatus().InstallState == AppInstallState.Error)
                {
                    tcs.TrySetException(new JobFailedException(installItem.GetCurrentStatus().ErrorCode.ToString()));
                }
                else if (installItem.GetCurrentStatus().InstallState == AppInstallState.Completed)
                {
                    if (!tcs.TrySetResult(true))
                    {
                        _logger.LogInformation($"{packageId} In StatusChanged handler, RanToCompleted already set.");
                    }
                    else
                    {
                        _logger.LogInformation($"{packageId} In StatusChanged handler, RanToCompleted set.");
                    }
                }
            };
            return tcs.Task;
        });
    }
}
