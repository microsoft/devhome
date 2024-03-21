// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Services;
using DevHome.Settings.ViewModels;
using DevHome.Settings.Views;

namespace DevHome.Settings.Extensions;

public static class PageExtensions
{
    public static void ConfigureSettingsPages(this IPageService pageService)
    {
        // Settings is not a Tool, so the main page is not configured automatically. Configure it here.
        pageService.Configure<SettingsViewModel, SettingsPage>();

        // Configure sub-pages
        pageService.Configure<PreferencesViewModel, PreferencesPage>();
        pageService.Configure<AccountsViewModel, AccountsPage>();
        pageService.Configure<ExperimentalFeaturesViewModel, ExperimentalFeaturesPage>();
        pageService.Configure<FeedbackViewModel, FeedbackPage>();
        pageService.Configure<AboutViewModel, AboutPage>();
    }
}
