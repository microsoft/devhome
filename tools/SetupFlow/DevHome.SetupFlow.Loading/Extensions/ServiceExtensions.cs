// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.Loading.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DevHome.SetupFlow.Loading.Extensions;
public static class ServiceExtensions
{
    public static IServiceCollection AddLoading(this IServiceCollection services)
    {
        // View models
        services.AddTransient<LoadingViewModel>();

        return services;
    }
}
