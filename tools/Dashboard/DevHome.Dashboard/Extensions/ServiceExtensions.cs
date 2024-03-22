// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Services;
using DevHome.Dashboard.Services;
using DevHome.Dashboard.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevHome.Dashboard.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDashboard(this IServiceCollection services, HostBuilderContext context)
    {
        // View-models
        services.AddSingleton<DashboardViewModel>();
        services.AddTransient<DashboardBannerViewModel>();
        services.AddTransient<AddWidgetViewModel>();

        // DI factory pattern for creating instances with certain parameters
        // determined at runtime
        services.AddSingleton<WidgetViewModelFactory>(
            sp => (widget, widgetSize, widgetDefinition) =>
                ActivatorUtilities.CreateInstance<WidgetViewModel>(sp, widget, widgetSize, widgetDefinition));

        // Services
        services.AddSingleton<IWidgetHostingService, WidgetHostingService>();
        services.AddSingleton<IWidgetIconService, WidgetIconService>();
        services.AddSingleton<IWidgetScreenshotService, WidgetScreenshotService>();
        services.AddSingleton<IAdaptiveCardRenderingService, AdaptiveCardRenderingService>();

        return services;
    }
}
