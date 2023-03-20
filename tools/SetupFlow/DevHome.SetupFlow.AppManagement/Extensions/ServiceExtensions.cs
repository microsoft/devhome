// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.AppManagement;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.SetupFlow.AppManagement.ViewModels;
using DevHome.SetupFlow.ComInterop.Projection.WindowsPackageManager;
using Microsoft.Extensions.DependencyInjection;

namespace DevHome.SetupFlow.Extensions;
public static class ServiceExtensions
{
    public static IServiceCollection AddAppManagement(this IServiceCollection services)
    {
        // View models
        services.AddTransient<ShimmerSearchViewModel>();
        services.AddTransient<SearchViewModel>();
        services.AddTransient<PackageViewModel>();
        services.AddTransient<PackageCatalogListViewModel>();
        services.AddTransient<AppManagementViewModel>();
        services.AddTransient<PackageCatalogViewModel>();
        services.AddTransient<AppManagementReviewViewModel>();

        // Services
        services.AddSingleton<IWindowsPackageManager, WindowsPackageManager>();
        services.AddSingleton(new WindowsPackageManagerFactory(ClsidContext.Prod));
        services.AddTransient<AppManagementTaskGroup>();
        services.AddSingleton<WinGetPackageJsonDataSource>();
        return services;
    }
}
