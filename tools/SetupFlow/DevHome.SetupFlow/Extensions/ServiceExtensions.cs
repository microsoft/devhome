// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.ViewModels;
using DevHome.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        services.AddSingleton(LoggerFactory.Get<ILogger>());
        services.AddSingleton<SetupFlowStringResource>();
        services.AddSingleton<SetupFlowOrchestrator>();

        // Configurations
        services.Configure<SetupFlowOptions>(context.Configuration.GetSection(nameof(SetupFlowOptions)));

        return services;
    }
}
