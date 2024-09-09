// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.RepositoryManagement.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevHome.RepositoryManagement.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddRepositoryManagement(this IServiceCollection services, HostBuilderContext context)
    {
        services.AddSingleton<RepositoryManagementMainPageViewModel>();
        services.AddTransient<RepositoryManagementItemViewModel>();

        return services;
    }
}
