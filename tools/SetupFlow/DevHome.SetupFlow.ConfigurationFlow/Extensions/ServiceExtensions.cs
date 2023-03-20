// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.ConfigurationFile.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DevHome.SetupFlow.ConfigurationFile.Extensions;
public static class ServiceExtensions
{
    public static IServiceCollection AddConfigurationFile(this IServiceCollection services)
    {
        // View models
        services.AddTransient<ConfigurationFileViewModel>();

        // Services
        services.AddTransient<ConfigurationFileTaskGroup>();

        return services;
    }
}
