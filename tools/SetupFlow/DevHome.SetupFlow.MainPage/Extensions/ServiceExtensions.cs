// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.MainPage.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DevHome.SetupFlow.Extensions;
public static class ServiceExtensions
{
    public static IServiceCollection AddMainPage(this IServiceCollection services)
    {
        // View models
        services.AddTransient<MainPageViewModel>();

        return services;
    }
}
