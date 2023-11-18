// Copyright(c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Environments.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevHome.Environments.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddEnvironments(this IServiceCollection services, HostBuilderContext context)
    {
        // View-models
        services.AddSingleton<EnvironmentsViewModel>();

        return services;
    }
}
