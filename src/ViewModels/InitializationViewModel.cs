// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using DevHome.Helpers;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using DevHome.Views;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.Store.Preview.InstallControl;

namespace DevHome.ViewModels;
public class InitializationViewModel : ObservableRecipient
{
    private const string GitHubPluginStorePackageId = "9NZCC27PR6N6";
    private const int StoreInstallTimeout = 60_000;

    private readonly IThemeSelectorService _themeSelector;

    public InitializationViewModel(IThemeSelectorService themeSelector)
    {
        _themeSelector = themeSelector;
    }

    public async Task OnPageLoadedAsync()
    {
        try
        {
            var appInstallTask = InstallStorePackageAsync(GitHubPluginStorePackageId);

            // wait for a maximunm of StoreInstallTimeout milis
            var completedTask = await Task.WhenAny(appInstallTask, Task.Delay(StoreInstallTimeout));

            if (completedTask.Exception is not null)
            {
                throw completedTask.Exception;
            }

            if (completedTask != appInstallTask)
            {
                throw new TimeoutException("Store Install task did not finish in time.");
            }
        }
        catch (Exception ex)
        {
            Log.Logger?.ReportError("GitHubExtension Hydration Failed", ex);
        }

        App.MainWindow.Content = Application.Current.GetService<ShellPage>();

        _themeSelector.SetRequestedTheme();
    }

    private async Task InstallStorePackageAsync(string packageId)
    {
        await Task.Run(() =>
        {
            var tcs = new TaskCompletionSource<bool>();

            AppInstallItem installItem;

            try
            {
                Log.Logger?.ReportInfo("Initialization Page: Starting extension app install");
                installItem = new AppInstallManager().StartAppInstallAsync(packageId, null, true, false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Logger?.ReportInfo("Initialization Page: Extension app install success");
                tcs.SetException(ex);
                return tcs.Task;
            }

            installItem.Completed += (sender, args) =>
            {
                tcs.SetResult(true);
            };

            installItem.StatusChanged += (sender, args) =>
            {
                if (installItem.GetCurrentStatus().InstallState == AppInstallState.Canceled
                    || installItem.GetCurrentStatus().InstallState == AppInstallState.Error)
                {
                    tcs.TrySetException(new JobFailedException(installItem.GetCurrentStatus().ErrorCode.ToString()));
                }
            };

            return tcs.Task;
        });
    }
}
