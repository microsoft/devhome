// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Settings.ViewModels;
using DevHome.Settings.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevHome.Settings.Extensions;
public static class ServiceExtensions
{
    public static IServiceCollection AddSettings(this IServiceCollection services, HostBuilderContext context)
    {
        // Project services
        services.AddTransient<FeedbackViewModel>();
        services.AddTransient<FeedbackPage>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<AboutViewModel>();
        services.AddTransient<AboutPage>();
        services.AddTransient<AccountsViewModel>();
        services.AddTransient<AccountsPage>();
        services.AddTransient<ExtensionsViewModel>();
        services.AddTransient<ExtensionsPage>();
        services.AddTransient<PreferencesViewModel>();
        services.AddTransient<PreferencesPage>();
        services.AddTransient<ExtensionSettingsViewModel>();
        services.AddTransient<ExtensionSettingsPage>();
        services.AddTransient<ExperimentalFeaturesViewModel>();

        return services;
    }
}
