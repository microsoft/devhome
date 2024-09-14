// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.RepositoryManagement.Factories;
using DevHome.RepositoryManagement.Services;
using DevHome.RepositoryManagement.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevHome.RepositoryManagement.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddRepositoryManagement(this IServiceCollection services, HostBuilderContext context)
    {
        services.AddSingleton<RepositoryManagementMainPageViewModel>();
        services.AddSingleton<RepositoryManagementItemViewModelFactory>();
        services.AddSingleton<EnhanceRepositoryService>();

        return services;
    }
}
