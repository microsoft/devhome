// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Services.WindowsPackageManager.Contracts;
using DevHome.Services.WindowsPackageManager.Contracts.Operations;
using DevHome.Services.WindowsPackageManager.Services;
using DevHome.Services.WindowsPackageManager.Services.Operations;
using Microsoft.Extensions.DependencyInjection;

namespace DevHome.Services.WindowsPackageManager.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddWindowsPackageManager(this IServiceCollection services)
    {
        services.AddCommon();
        return services;
    }

    private static IServiceCollection AddCommon(this IServiceCollection services)
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

        // services.AddSingleton<IDesiredStateConfiguration, DesiredStateConfiguration>();
        return services;
    }
}
