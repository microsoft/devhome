// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Customization.ViewModels;
using DevHome.Customization.Views;
using DevHome.QuietBackgroundProcesses.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevHome.Customization.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddWindowsCustomization(this IServiceCollection services, HostBuilderContext context)
    {
        services.AddSingleton<MainPageViewModel>();
        services.AddTransient<MainPage>();

<<<<<<< Updated upstream
        services.AddSingleton<DeveloperFileExplorerViewModel>();
        services.AddTransient<DeveloperFileExplorerPage>();
=======
        services.AddSingleton<FileExplorerViewModel>();
        services.AddTransient<FileExplorerPage>();

        services.AddSingleton<OptimizeDevDriveDialogViewModelFactory>(sp =>
            (cacheLocation, environmentVariable, exampleDevDriveLocation, existingDevDriveLetters) =>
                ActivatorUtilities.CreateInstance<OptimizeDevDriveDialogViewModel>(sp, cacheLocation, environmentVariable, exampleDevDriveLocation, existingDevDriveLetters));
        services.AddSingleton<DevDriveInsightsViewModel>();
        services.AddTransient<DevDriveInsightsPage>();

        services.AddTransient<QuietBackgroundProcessesViewModel>();
>>>>>>> Stashed changes

        return services;
    }
}
