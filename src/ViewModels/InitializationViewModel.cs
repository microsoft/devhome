// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Management.Automation;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using DevHome.Logging;
using DevHome.Views;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.Store.Preview.InstallControl;

namespace DevHome.ViewModels;
public class InitializationViewModel : ObservableObject
{
#if CANARY_BUILD
    private const string GitHubExtensionStorePackageId = "9N806ZKPW85R";
    private const string GitHubExtensionPackageFamilyName = "Microsoft.Windows.DevHomeGitHubExtension.Canary_8wekyb3d8bbwe";
#elif STABLE_BUILD
    private const string GitHubExtensionStorePackageId = "9NZCC27PR6N6";
    private const string GitHubExtensionPackageFamilyName = "Microsoft.Windows.DevHomeGitHubExtension_8wekyb3d8bbwe";
#else
    private const string GitHubExtensionStorePackageId = "";
    private const string GitHubExtensionPackageFamilyName = "";
#endif

    private const int StoreInstallTimeout = 60_000;

    private readonly IThemeSelectorService _themeSelector;
    private readonly IExtensionService _extensionService;

    public InitializationViewModel(IThemeSelectorService themeSelector, IExtensionService extensionService)
    {
        _themeSelector = themeSelector;
        _extensionService = extensionService;
    }

    public async Task OnPageLoadedAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(GitHubExtensionStorePackageId) &&
                !(await _extensionService.GetInstalledAppExtensionsAsync())
                .Any(extension => extension.AppInfo.PackageFamilyName.Equals(GitHubExtensionPackageFamilyName, StringComparison.OrdinalIgnoreCase)))
            {
                var appInstallTask = InstallStorePackageAsync(GitHubExtensionStorePackageId);

                // wait for a maximum of StoreInstallTimeout milliseconds
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
        }
        catch (Exception ex)
        {
            GlobalLog.Logger?.ReportError("GitHubExtension Hydration Failed", ex);
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
                GlobalLog.Logger?.ReportInfo("Initialization Page: Starting extension app install");
                installItem = new AppInstallManager().StartAppInstallAsync(packageId, null, true, false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                GlobalLog.Logger?.ReportInfo("Initialization Page: Extension app install success");
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
                else if (installItem.GetCurrentStatus().InstallState == AppInstallState.Completed)
                {
                    tcs.SetResult(true);
                }
            };

            return tcs.Task;
        });
    }
}
