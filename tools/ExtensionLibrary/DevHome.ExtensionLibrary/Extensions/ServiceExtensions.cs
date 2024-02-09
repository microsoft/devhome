// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.ExtensionLibrary.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevHome.ExtensionLibrary.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddExtensionLibrary(this IServiceCollection services, HostBuilderContext context)
    {
        // View-models
        services.AddTransient<ExtensionLibraryViewModel>();
        services.AddTransient<ExtensionLibraryBannerViewModel>();

        return services;
    }
}
