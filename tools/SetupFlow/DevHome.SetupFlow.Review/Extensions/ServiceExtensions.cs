// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.Review.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DevHome.SetupFlow.Review.Extensions;
public static class ServiceExtensions
{
    public static IServiceCollection AddReview(this IServiceCollection services)
    {
        // View models
        services.AddTransient<ReviewViewModel>();

        return services;
    }
}
