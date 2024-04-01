// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Customization.ViewModels;
using DevHome.Customization.ViewModels.DevDriveInsights;
using DevHome.Customization.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevHome.Customization.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddWindowsCustomization(this IServiceCollection services, HostBuilderContext context)
    {
        services.AddSingleton<MainPageViewModel>();
        services.AddTransient<MainPage>();

        services.AddSingleton<DeveloperFileExplorerViewModel>();
        services.AddTransient<DeveloperFileExplorerPage>();

        services.AddSingleton<OptimizeDevDriveDialogViewModelFactory>(sp => (cacheLocation, environmentVariable) => ActivatorUtilities.CreateInstance<OptimizeDevDriveDialogViewModel>(sp, cacheLocation, environmentVariable));
        services.AddSingleton<DevDriveInsightsViewModel>();
        services.AddTransient<DevDriveInsightsPage>();

        return services;
    }
}
