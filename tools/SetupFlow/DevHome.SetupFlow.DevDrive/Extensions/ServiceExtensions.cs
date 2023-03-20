// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.DevDrive;
using DevHome.SetupFlow.DevDrive.Services;
using DevHome.SetupFlow.DevDrive.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DevHome.SetupFlow.Extensions;
public static class ServiceExtensions
{
    public static IServiceCollection AddDevDrive(this IServiceCollection services)
    {
        // View models
        services.AddTransient<DevDriveViewModel>();
        services.AddTransient<DevDriveReviewViewModel>();

        // Services
        services.AddTransient<DevDriveTaskGroup>();
        services.AddSingleton<IDevDriveManager, DevDriveManager>();

        return services;
    }
}
