// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.Services.WinGet;
using DevHome.SetupFlow.Services.WinGet.Operations;
using DevHome.SetupFlow.TaskGroups;
using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.ViewModels.Environments;
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
        services.AddSetupTarget();
        services.AddAppManagement();
        services.AddConfigurationFile();
        services.AddDevDrive();
        services.AddLoading();
        services.AddMainPage();
        services.AddRepoConfig();
        services.AddCreateEnvironment();
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

    private static IServiceCollection AddWinGet(this IServiceCollection services)
    {
        services.AddSingleton<IWinGetCatalogConnector, WinGetCatalogConnector>();
        services.AddSingleton<IWinGetPackageFinder, WinGetPackageFinder>();
        services.AddSingleton<IWinGetPackageInstaller, WinGetPackageInstaller>();
        services.AddSingleton<IWinGetProtocolParser, WinGetProtocolParser>();
        services.AddSingleton<IWinGetDeployment, WinGetDeployment>();
        services.AddSingleton<IWinGetRecovery, WinGetRecovery>();
        services.AddSingleton<IWinGetPackageCache, WinGetPackageCache>();
        services.AddSingleton<IWinGetOperations, WinGetOperations>();
        services.AddSingleton<IWinGetGetPackageOperation, WinGetGetPackageOperation>();
        services.AddSingleton<IWinGetSearchOperation, WinGetSearchOperation>();
        services.AddSingleton<IWinGetInstallOperation, WinGetInstallOperation>();
        services.AddSingleton<IDesiredStateConfiguration, DesiredStateConfiguration>();
        return services;
    }

    private static IServiceCollection AddAppManagement(this IServiceCollection services)
    {
        // View models
        services.AddTransient<ShimmerSearchViewModel>();
        services.AddTransient<SearchViewModel>();
        services.AddTransient<PackageCatalogListViewModel>();
        services.AddTransient<AppManagementViewModel>();
        services.AddTransient<AppManagementReviewViewModel>();

        // Services
        services.AddSingleton<IWindowsPackageManager, WindowsPackageManager>();
        services.AddSingleton<WindowsPackageManagerFactory>(new WindowsPackageManagerDefaultFactory(ClsidContext.Prod));
        services.AddSingleton<IRestoreInfo, RestoreInfo>();
        services.AddSingleton<PackageProvider>();
        services.AddTransient<AppManagementTaskGroup>();
        services.AddSingleton<ICatalogDataSourceLoader, CatalogDataSourceLoader>();
        services.AddSingleton<IAppManagementInitializer, AppManagementInitializer>();
        services.AddWinGet();

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

        // Builder
        services.AddSingleton<ConfigurationFileBuilder>();

        return services;
    }

    private static IServiceCollection AddDevDrive(this IServiceCollection services)
    {
        // View models
        services.AddTransient<DevDriveViewModel>();

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
        services.AddTransient<MainPageBannerViewModel>();

        return services;
    }

    private static IServiceCollection AddRepoConfig(this IServiceCollection services)
    {
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

    private static IServiceCollection AddSetupTarget(this IServiceCollection services)
    {
        // View models
        services.AddSingleton<ComputeSystemViewModelFactory>();
        services.AddTransient<SetupTargetViewModel>();
        services.AddTransient<SetupTargetReviewViewModel>();
        services.AddTransient<SetupTargetTaskGroup>();

        return services;
    }

    private static IServiceCollection AddCreateEnvironment(this IServiceCollection services)
    {
        // View models
        services.AddTransient<EnvironmentCreationOptionsTaskGroup>();
        services.AddTransient<SelectEnvironmentProviderTaskGroup>();
        services.AddTransient<CreateEnvironmentReviewViewModel>();
        services.AddTransient<EnvironmentCreationOptionsViewModel>();
        services.AddTransient<SelectEnvironmentProviderViewModel>();

        return services;
    }
}
