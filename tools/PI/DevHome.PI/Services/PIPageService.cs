// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Services;
using DevHome.PI.Pages;
using DevHome.PI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.PI.Services;

// Similar to DevHome.Services.PageService
internal sealed class PIPageService : IPageService
{
    private readonly Dictionary<string, Type> pages = new();

    public PIPageService()
    {
        Configure<AppDetailsPageViewModel, AppDetailsPage>();
        Configure<InsightsPageViewModel, InsightsPage>();
        Configure<ModulesPageViewModel, ModulesPage>();
        Configure<ProcessListPageViewModel, ProcessListPage>();
        Configure<ResourceUsagePageViewModel, ResourceUsagePage>();
        Configure<WERPageViewModel, WERPage>();
        Configure<WinLogsPageViewModel, WinLogsPage>();
        Configure<SettingsPageViewModel, SettingsPage>();

        // Settings sub-pages.
        Configure<PreferencesViewModel, PreferencesPage>();
        Configure<AdditionalToolsViewModel, AdditionalToolsPage>();
        Configure<AdvancedSettingsViewModel, AdvancedSettingsPage>();
        Configure<AboutViewModel, AboutPage>();
    }

    public Type GetPageType(string key)
    {
        Type? pageType;
        lock (pages)
        {
            if (!pages.TryGetValue(key, out pageType))
            {
                throw new ArgumentException($"Page not found: {key}. Did you forget to call PageService.Configure?");
            }
        }

        return pageType;
    }

    public void Configure<T_VM, T_V>()
        where T_VM : ObservableObject
        where T_V : Page
    {
        lock (pages)
        {
            var key = typeof(T_VM).FullName!;
            if (pages.ContainsKey(key))
            {
                throw new ArgumentException($"The key {key} is already configured in PageService");
            }

            var type = typeof(T_V);
            if (pages.Any(p => p.Value == type))
            {
                throw new ArgumentException($"This type is already configured with key {pages.First(p => p.Value == type).Key}");
            }

            pages.Add(key, type);
        }
    }
}
