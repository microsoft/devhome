// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.AppManagement.Extensions;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.ConfigurationFile.Extensions;
using DevHome.SetupFlow.DevDrive.Extensions;
using DevHome.SetupFlow.Loading.Extensions;
using DevHome.SetupFlow.MainPage.Extensions;
using DevHome.SetupFlow.RepoConfig.Extensions;
using DevHome.SetupFlow.Review.Extensions;
using DevHome.SetupFlow.Summary.Extensions;
using DevHome.SetupFlow.ViewModels;
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
        services.AddSingleton<ISetupFlowStringResource, SetupFlowStringResource>();
        services.AddSingleton<SetupFlowOrchestrator>();

        // Configurations
        services.Configure<SetupFlowOptions>(context.Configuration.GetSection(nameof(SetupFlowOptions)));

        return services;
    }
}
