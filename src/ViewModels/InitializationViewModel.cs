// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using DevHome.Dashboard.Services;
using DevHome.Logging;
using DevHome.Services;
using DevHome.Views;
using Microsoft.UI.Xaml;

namespace DevHome.ViewModels;

public class InitializationViewModel : ObservableObject
{
    private readonly IThemeSelectorService _themeSelector;
    private readonly IWidgetHostingService _widgetHostingService;
    private readonly IAppInstallManagerService _appInstallManagerService;
    private readonly IPackageDeploymentService _packageDeploymentService;

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

    public InitializationViewModel(
        IThemeSelectorService themeSelector,
        IWidgetHostingService widgetHostingService,
        IAppInstallManagerService appInstallManagerService,
        IPackageDeploymentService packageDeploymentService)
    {
        _themeSelector = themeSelector;
        _widgetHostingService = widgetHostingService;
        _appInstallManagerService = appInstallManagerService;
        _packageDeploymentService = packageDeploymentService;
    }

    public async void OnPageLoaded()
    {
        // Install the widget service if we're on Windows 10 and it's not already installed.
        try
        {
            if (_widgetHostingService.CheckForWidgetServiceAsync())
            {
                GlobalLog.Logger?.ReportInfo("InitializationViewModel", "Skipping installing WidgetService, already installed.");
            }
            else
            {
                if (_widgetHostingService.GetWidgetServiceState() == WidgetHostingService.WidgetServiceStates.HasStoreWidgetServiceNoOrBadVersion)
                {
                    // We're on Windows 10 and don't have the widget service, try to install it.
                    await _widgetHostingService.TryInstallingWidgetService();
                }
            }
        }
        catch (Exception ex)
        {
            GlobalLog.Logger?.ReportInfo("InitializationViewModel", "Installing WidgetService failed: ", ex);
        }

        // Install the DevHomeGitHubExtension, unless it's already installed or a dev build is running.
        if (string.IsNullOrEmpty(GitHubExtensionStorePackageId) || HasDevHomeGitHubExtensionInstalled())
        {
            GlobalLog.Logger?.ReportInfo("InitializationViewModel", "Skipping installing DevHomeGitHubExtension.");
        }
        else
        {
            try
            {
                GlobalLog.Logger?.ReportInfo("InitializationViewModel", "Installing DevHomeGitHubExtension...");
                await _appInstallManagerService.TryInstallPackageAsync(GitHubExtensionStorePackageId);
            }
            catch (Exception ex)
            {
                GlobalLog.Logger?.ReportInfo("InitializationViewModel", "Installing DevHomeGitHubExtension failed: ", ex);
            }
        }

        App.MainWindow.Content = Application.Current.GetService<ShellPage>();

        _themeSelector.SetRequestedTheme();
    }

    private bool HasDevHomeGitHubExtensionInstalled()
    {
        var packages = _packageDeploymentService.FindPackagesForCurrentUser(GitHubExtensionPackageFamilyName);
        return packages.Any();
    }
}
