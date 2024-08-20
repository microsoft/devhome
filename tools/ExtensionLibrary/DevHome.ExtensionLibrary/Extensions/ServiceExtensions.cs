// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.ExtensionLibrary.ViewModels;
using DevHome.ExtensionLibrary.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevHome.ExtensionLibrary.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddExtensionLibrary(this IServiceCollection services, HostBuilderContext context)
    {
        services.AddTransient<ExtensionLibraryViewModel>();
        services.AddTransient<ExtensionLibraryBannerViewModel>();

        services.AddTransient<ExtensionSettingsViewModel>();
        services.AddTransient<ExtensionSettingsPage>();

        services.AddTransient<ExtensionNavigationViewModel>();
        services.AddTransient<ExtensionNavigationPage>();

        return services;
    }
}
