// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.RepositoryManagement.Services;
using DevHome.RepositoryManagement.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevHome.RepositoryManagement.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddRepositoryManagement(this IServiceCollection services, HostBuilderContext context)
    {
        services.AddSingleton<RepositoryManagementDataAccessService>();
        services.AddSingleton<RepositoryManagementMainPageViewModel>();
        services.AddTransient<RepositoryManagementItemViewModel>();

        return services;
    }
}
