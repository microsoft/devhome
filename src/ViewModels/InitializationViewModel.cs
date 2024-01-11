// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
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

#if CANARY_BUILD
    private const string GitHubExtensionStorePackageId = "9N806ZKPW85R";
#elif STABLE_BUILD
    private const string GitHubExtensionStorePackageId = "9NZCC27PR6N6";
#else
    private const string GitHubExtensionStorePackageId = "";
#endif

    public InitializationViewModel(IThemeSelectorService themeSelector, IWidgetHostingService widgetHostingService, IAppInstallManagerService appInstallManagerService)
    {
        _themeSelector = themeSelector;
        _widgetHostingService = widgetHostingService;
        _appInstallManagerService = appInstallManagerService;
    }

    public async void OnPageLoaded()
    {
        try
        {
            await _widgetHostingService.EnsureWidgetServiceAsync();
        }
        catch (Exception ex)
        {
            GlobalLog.Logger?.ReportInfo("InitializationViewModel", "Installing WidgetService failed: ", ex);
        }

        if (string.IsNullOrEmpty(GitHubExtensionStorePackageId))
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
}
