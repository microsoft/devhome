// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Environments.Helpers;
using DevHome.Environments.Models;
using DevHome.Environments.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.ExtensionLibrary.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddEnvironments(this IServiceCollection services, HostBuilderContext context)
    {
        // View-models
        services.AddSingleton<LandingPageViewModel>();

#if DEBUG
        // Debug only offline test compute system provider
        services.AddSingleton<IComputeSystemProvider, TestSystemProvider>();
#endif

        return services;
    }
}
