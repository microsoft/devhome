// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.IO;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;
using DevHome.SetupFlow.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Internal.Windows.DevHome.Helpers;
using Microsoft.Internal.Windows.DevHome.Helpers.Restore;

namespace DevHome.SetupFlow.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddSetupFlow(this IServiceCollection services, HostBuilderContext context)
    {
        // Project services
        services.AddAppManagement();
        services.AddConfigurationFile();
        services.AddDevDrive();
        services.AddLoading();
        services.AddMainPage();
        services.AddRepoConfig();
        services.AddReview();
        services.AddSummary();

        // View-models
        services.AddSingleton<SetupFlowViewModel>();

        // Services
        services.AddSingleton<ISetupFlowStringResource, SetupFlowStringResource>();
        services.AddSingleton<SetupFlowOrchestrator>();

        // Configurations
        services.Configure<SetupFlowOptions>(context.Configuration.GetSection(nameof(SetupFlowOptions)));

        return services;
    }

    private static IServiceCollection AddAppManagement(this IServiceCollection services)
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
        services.AddSingleton<WindowsPackageManagerFactory>(new WindowsPackageManagerDefaultFactory(ClsidContext.Prod));
        services.AddSingleton<IRestoreInfo, RestoreInfo>();
        services.AddSingleton<PackageProvider>();
        services.AddTransient<AppManagementTaskGroup>();
        services.AddSingleton<CatalogDataSourceLoacder>();
        services.AddSingleton<IAppManagementInitializer, AppManagementInitializer>();

        services.AddSingleton<WinGetPackageDataSource, WinGetPackageRestoreDataSource>();
        services.AddSingleton<WinGetPackageDataSource,  WinGetPackageJsonDataSource>(sp =>
        {
            var dataSourcePath = sp.GetService<IOptions<SetupFlowOptions>>().Value.WinGetPackageJsonDataSourcePath;
            var dataSourceFullPath = Path.Combine(AppContext.BaseDirectory, dataSourcePath);
            return ActivatorUtilities.CreateInstance<WinGetPackageJsonDataSource>(sp, dataSourceFullPath);
        });
        services.AddSingleton<WinGetPackageDataSource, WinGetFeaturedApplicationsDataSource>();

        // DI factory pattern for creating instances with certain parameters
        // determined at runtime
        services.AddSingleton<PackageViewModelFactory>(sp => package => ActivatorUtilities.CreateInstance<PackageViewModel>(sp, package));
        services.AddSingleton<PackageCatalogViewModelFactory>(sp => catalog => ActivatorUtilities.CreateInstance<PackageCatalogViewModel>(sp, catalog));
        services.AddSingleton<ConfigurationUnitResultViewModelFactory>(sp => unitResult => ActivatorUtilities.CreateInstance<ConfigurationUnitResultViewModel>(sp, unitResult));

        return services;
    }

    private static IServiceCollection AddConfigurationFile(this IServiceCollection services)
    {
        // View models
        services.AddTransient<ConfigurationFileViewModel>();

        // Services
        services.AddTransient<ConfigurationFileTaskGroup>();

        return services;
    }

    private static IServiceCollection AddDevDrive(this IServiceCollection services)
    {
        // View models
        services.AddTransient<DevDriveViewModel>();
        services.AddTransient<DevDriveReviewViewModel>();

        // Services
        services.AddTransient<DevDriveTaskGroup>();
        services.AddSingleton<IDevDriveManager, DevDriveManager>();

        return services;
    }

    private static IServiceCollection AddLoading(this IServiceCollection services)
    {
        // View models
        services.AddTransient<LoadingViewModel>();

        return services;
    }

    private static IServiceCollection AddMainPage(this IServiceCollection services)
    {
        // View models
        services.AddTransient<MainPageViewModel>();

        return services;
    }

    private static IServiceCollection AddRepoConfig(this IServiceCollection services)
    {
        // View models
        services.AddTransient<AddRepoViewModel>();
        services.AddTransient<RepoConfigViewModel>();
        services.AddTransient<RepoConfigReviewViewModel>();

        // Services
        services.AddTransient<RepoConfigTaskGroup>();

        return services;
    }

    private static IServiceCollection AddReview(this IServiceCollection services)
    {
        // View models
        services.AddTransient<ReviewViewModel>();

        return services;
    }

    private static IServiceCollection AddSummary(this IServiceCollection services)
    {
        // View models
        services.AddTransient<SummaryViewModel>();

        return services;
    }
}
