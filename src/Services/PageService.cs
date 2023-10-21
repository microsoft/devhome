// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using DevHome.Settings;
using DevHome.Settings.ViewModels;
using DevHome.Settings.Views;
using DevHome.ViewModels;
using DevHome.Views;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Services;

public class PageService : IPageService
{
    private readonly Dictionary<string, Type> _pages = new ();

    public PageService()
    {
        Configure<SettingsViewModel, SettingsPage>();
        Configure<PreferencesViewModel, PreferencesPage>();
        Configure<AccountsViewModel, AccountsPage>();
        Configure<ExtensionsViewModel, ExtensionsPage>();
        Configure<AboutViewModel, AboutPage>();
        Configure<FeedbackViewModel, FeedbackPage>();
        Configure<WhatsNewViewModel, WhatsNewPage>();
        Configure<ExtensionSettingsViewModel, ExtensionSettingsPage>();
        Configure<ExperimentalFeaturesViewModel, ExperimentalFeaturesPage>();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var group in App.NavConfig.NavMenu.Groups)
        {
            foreach (var tool in group.Tools)
            {
                var toolType = from assembly in assemblies
                               where assembly.GetName().Name == tool.Assembly
                               select assembly.GetType(tool.ViewFullName);

                Configure(tool.ViewModelFullName, toolType.First());
            }
        }

        var experimentalFeaturesVM = App.Current.GetService<ExperimentalFeaturesViewModel>();
        foreach (var experimentId in App.NavConfig.ExperimentIds ?? Array.Empty<string>())
        {
            experimentalFeaturesVM.Features.Add(new ExperimentalFeature(experimentId));
        }
    }

    public Type GetPageType(string key)
    {
        Type? pageType;
        lock (_pages)
        {
            if (!_pages.TryGetValue(key, out pageType))
            {
                throw new ArgumentException($"Page not found: {key}. Did you forget to call PageService.Configure?");
            }
        }

        return pageType;
    }

    private void Configure<T_VM, T_V>()
        where T_VM : ObservableObject
        where T_V : Page
    {
        lock (_pages)
        {
            var key = typeof(T_VM).FullName!;
            if (_pages.ContainsKey(key))
            {
                throw new ArgumentException($"The key {key} is already configured in PageService");
            }

            var type = typeof(T_V);
            if (_pages.Any(p => p.Value == type))
            {
                throw new ArgumentException($"This type is already configured with key {_pages.First(p => p.Value == type).Key}");
            }

            _pages.Add(key, type);
        }
    }

    private void Configure(string t_vm, Type t_v)
    {
        lock (_pages)
        {
            if (_pages.ContainsKey(t_vm))
            {
                throw new ArgumentException($"The key {t_vm} is already configured in PageService");
            }

            if (_pages.Any(p => p.Value == t_v))
            {
                throw new ArgumentException($"This type is already configured with key {_pages.First(p => p.Value == t_v).Key}");
            }

            _pages.Add(t_vm, t_v);
        }
    }
}
