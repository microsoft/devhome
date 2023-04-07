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

    private readonly ILogger _logger;
    private readonly IThemeSelectorService _themeSelector;

    public InitializationViewModel(ILogger logger, IThemeSelectorService themeSelector)
    {
        _logger = logger;
        _themeSelector = themeSelector;
    }

    public async Task OnPageLoadedAsync()
    {
        try
        {
            var appInstallTask = InstallStorePackageAsync(GitHubPluginStorePackageId);

            // wait for a maximunm of StoreInstallTimeout milis
            if (await Task.WhenAny(appInstallTask, Task.Delay(StoreInstallTimeout)) != appInstallTask)
            {
                throw new TimeoutException("Store Install task did not finish in time.");
            }
        }
        catch (Exception ex)
        {
            _logger.Log("GitHubExtension Hydration Failed", LogLevel.Local, ex);
        }

        App.MainWindow.Content = Application.Current.GetService<ShellPage>();

        _themeSelector.SetRequestedTheme();
    }

    private async Task InstallStorePackageAsync(string packageId)
    {
        await Task.Run(() =>
        {
            var tcs = new TaskCompletionSource<bool>();

            var install = new AppInstallManager().StartAppInstallAsync(packageId, null, true, false).GetAwaiter().GetResult();

            install.Completed += (sender, args) =>
            {
                tcs.SetResult(true);
            };

            install.StatusChanged += (sender, args) =>
            {
                if (install.GetCurrentStatus().InstallState == AppInstallState.Canceled
                    || install.GetCurrentStatus().InstallState == AppInstallState.Error)
                {
                    tcs.TrySetException(new JobFailedException(install.GetCurrentStatus().ErrorCode.ToString()));
                }
            };

            return tcs.Task;
        });
    }
}
