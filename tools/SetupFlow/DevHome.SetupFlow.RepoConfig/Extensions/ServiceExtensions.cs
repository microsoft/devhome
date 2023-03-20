// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.RepoConfig.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DevHome.SetupFlow.RepoConfig.Extensions;
public static class ServiceExtensions
{
    public static IServiceCollection AddRepoConfig(this IServiceCollection services)
    {
        // View models
        services.AddTransient<AddRepoViewModel>();
        services.AddTransient<RepoConfigViewModel>();
        services.AddTransient<RepoConfigReviewViewModel>();

        // Services
        services.AddTransient<RepoConfigTaskGroup>();

        return services;
    }
}
