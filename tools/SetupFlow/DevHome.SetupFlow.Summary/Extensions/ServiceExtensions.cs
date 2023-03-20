// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.Summary.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DevHome.SetupFlow.Extensions;
public static class ServiceExtensions
{
    public static IServiceCollection AddSummary(this IServiceCollection services)
    {
        // View models
        services.AddTransient<SummaryViewModel>();

        return services;
    }
}
