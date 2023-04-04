// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.IO;
using DevHome.Common.Services;
using DevHome.SetupFlow.AppManagement;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.SetupFlow.AppManagement.ViewModels;
using DevHome.SetupFlow.ComInterop.Projection.WindowsPackageManager;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.ConfigurationFile;
using DevHome.SetupFlow.ConfigurationFile.ViewModels;
using DevHome.SetupFlow.DevDrive;
using DevHome.SetupFlow.DevDrive.Services;
using DevHome.SetupFlow.DevDrive.ViewModels;
using DevHome.SetupFlow.Loading.ViewModels;
using DevHome.SetupFlow.MainPage.ViewModels;
using DevHome.SetupFlow.RepoConfig;
using DevHome.SetupFlow.RepoConfig.ViewModels;
using DevHome.SetupFlow.Review.ViewModels;
using DevHome.SetupFlow.Summary.ViewModels;
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
        services.AddTransient<SetupFlowViewModel>();

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
        services.AddTransient<WinGetPackageRestoreDataSource>();
        services.AddTransient<WinGetPackageJsonDataSource>(sp =>
        {
            var dataSourcePath = sp.GetService<IOptions<SetupFlowOptions>>().Value.WinGetPackageJsonDataSourcePath;
            var dataSourceFullPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, dataSourcePath);
            return ActivatorUtilities.CreateInstance<WinGetPackageJsonDataSource>(sp, dataSourceFullPath);
        });

        // DI factory pattern
        services.AddSingleton<PackageViewModelFactory>(sp => package => ActivatorUtilities.CreateInstance<PackageViewModel>(sp, package));
        services.AddSingleton<PackageCatalogViewModelFactory>(sp => catalog => ActivatorUtilities.CreateInstance<PackageCatalogViewModel>(sp, catalog));

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
