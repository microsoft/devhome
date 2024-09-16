// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Contracts;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Customization.Extensions;
using DevHome.ExtensionLibrary.Extensions;
using DevHome.Settings.Extensions;
using DevHome.ViewModels;
using DevHome.Views;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Services;

public class PageService : IPageService
{
#if CANARY_BUILD
    private const string BuildType = "canary";
#elif STABLE_BUILD
    private const string BuildType = "stable";
#else
    private const string BuildType = "dev";
#endif

    private readonly Dictionary<string, Type> _pages = new();

    public PageService(ILocalSettingsService localSettingsService, IExperimentationService experimentationService, IQuickstartSetupService quickstartSetupService)
    {
        // Configure top-level pages from registered tools
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

        // Configure nested pages from tools
        this.ConfigureCustomizationPages();

        // Configure footer pages
        Configure<WhatsNewViewModel, WhatsNewPage>();
        this.ConfigureExtensionLibraryPages();
        this.ConfigureSettingsPages();

        // Configure Experimental Feature pages
        ExperimentalFeature.LocalSettingsService = localSettingsService;
        ExperimentalFeature.QuickstartSetupService = quickstartSetupService;
        foreach (var experimentalFeature in App.NavConfig.ExperimentFeatures ?? Array.Empty<DevHome.Helpers.ExperimentalFeatures>())
        {
            var enabledByDefault = experimentalFeature.EnabledByDefault;
            var needsFeaturePresenceCheck = experimentalFeature.NeedsFeaturePresenceCheck;
            var openPageKey = experimentalFeature.OpenPage.Key;
            var openPageParameter = experimentalFeature.OpenPage.Parameter;
            var isVisible = true;
            foreach (var buildTypeOverride in experimentalFeature.BuildTypeOverrides ?? Array.Empty<DevHome.Helpers.BuildTypeOverrides>())
            {
                if (buildTypeOverride.BuildType == BuildType)
                {
                    enabledByDefault = buildTypeOverride.EnabledByDefault;
                    isVisible = buildTypeOverride.Visible;
                    break;
                }
            }

            experimentationService.AddExperimentalFeature(new ExperimentalFeature(experimentalFeature.Identity, enabledByDefault, needsFeaturePresenceCheck, openPageKey, openPageParameter, isVisible));
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

    public void Configure<T_VM, T_V>()
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
