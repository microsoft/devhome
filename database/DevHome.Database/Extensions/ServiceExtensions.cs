// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Database.Factories;
using DevHome.Database.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevHome.Database.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, HostBuilderContext context)
    {
        services.AddSingleton<DevHomeDatabaseContextFactory>();

        services.AddSingleton<RepositoryManagementDataAccessService>();

        return services;
    }
}
