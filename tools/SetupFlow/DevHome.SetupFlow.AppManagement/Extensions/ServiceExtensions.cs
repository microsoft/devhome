// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.IO;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.SetupFlow.AppManagement.ViewModels;
using DevHome.SetupFlow.ComInterop.Projection.WindowsPackageManager;
using DevHome.SetupFlow.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Internal.Windows.DevHome.Helpers;
using Microsoft.Internal.Windows.DevHome.Helpers.Restore;

namespace DevHome.SetupFlow.AppManagement.Extensions;
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

        return services;
    }
}
