// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Serilog;
using Windows.ApplicationModel.Store.Preview.InstallControl;
using Windows.Foundation;

namespace DevHome.Services;

/// <summary>
/// Service class for using the Store API <see cref="AppInstallManager"/>
/// </summary>
public class AppInstallManagerService : IAppInstallManagerService
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(AppInstallManagerService));

    private readonly AppInstallManager _appInstallManager;

    private static readonly TimeSpan _storeInstallTimeout = new(0, 0, 60);

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
            _log.Error(ex, "Package installation Failed");
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
                _log.Information($"Starting {packageId} install");
                installItem = _appInstallManager.StartAppInstallAsync(packageId, null, true, false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error($"{packageId} install failure");
                tcs.SetException(ex);
                return tcs.Task;
            }

            installItem.Completed += (sender, args) =>
            {
                if (!tcs.TrySetResult(true))
                {
                    _log.Information("WidgetHostingService", $"{packageId} In Completed handler, RanToCompleted already set.");
                }
                else
                {
                    _log.Information("WidgetHostingService", $"{packageId} In Completed handler, RanToCompleted set.");
                }
            };

            installItem.StatusChanged += (sender, args) =>
            {
                if (installItem.GetCurrentStatus().InstallState == AppInstallState.Canceled
                    || installItem.GetCurrentStatus().InstallState == AppInstallState.Error)
                {
                    tcs.TrySetException(new System.Management.Automation.JobFailedException(installItem.GetCurrentStatus().ErrorCode.ToString()));
                }
                else if (installItem.GetCurrentStatus().InstallState == AppInstallState.Completed)
                {
                    if (!tcs.TrySetResult(true))
                    {
                        _log.Information("WidgetHostingService", $"{packageId} In StatusChanged handler, RanToCompleted already set.");
                    }
                    else
                    {
                        _log.Information("WidgetHostingService", $"{packageId} In StatusChanged handler, RanToCompleted set.");
                    }
                }
            };
            return tcs.Task;
        });
    }
}
