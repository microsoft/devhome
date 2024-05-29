// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Utilities.ViewModels;
using DevHome.Utilities.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevHome.Utilities.Extensions;

public static class ServiceExtensions
{
    // This needs to be called from App() in App.xaml.cs
    public static IServiceCollection AddUtilities(this IServiceCollection services, HostBuilderContext context)
    {
        // View-models
        services.AddSingleton<UtilitiesMainPageViewModel>();
        services.AddTransient<UtilitiesMainPageView>();

        return services;
    }
}
