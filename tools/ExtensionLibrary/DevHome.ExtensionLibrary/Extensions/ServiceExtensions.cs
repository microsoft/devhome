// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.ExtensionLibrary.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevHome.Settings.Extensions;
public static class ServiceExtensions
{
    public static IServiceCollection AddExtensionLibrary(this IServiceCollection services, HostBuilderContext context)
    {
        // View-models
        services.AddTransient<ExtensionLibraryViewModel>();

        return services;
    }
}
