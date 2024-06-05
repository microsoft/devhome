// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.PI.Controls;
using DevHome.PI.Pages;
using DevHome.PI.Services;
using DevHome.PI.Telemetry;
using DevHome.PI.TelemetryEvents;
using DevHome.PI.ViewModels;
using DevHome.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace DevHome.PI;

public partial class App : Application, IApp
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host { get; }

    public T GetService<T>()
        where T : class => Host.GetService<T>();

    public Microsoft.UI.Dispatching.DispatcherQueue? UIDispatcher { get; }

    public App()
    {
        InitializeComponent();

        UIDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Services
                services.AddSingleton<IPageService, PIPageService>();
                services.AddSingleton<INavigationService, PINavigationService>();
                services.AddSingleton<TelemetryReporter>();
                services.AddSingleton<PIAppInfoService>();

                // Window
                services.AddSingleton<PrimaryWindow>();

                // Views and ViewModels
                services.AddSingleton<AppDetailsPage>();
                services.AddSingleton<AppDetailsPageViewModel>();
                services.AddSingleton<InsightsPage>();
                services.AddSingleton<InsightsPageViewModel>();
                services.AddSingleton<ModulesPage>();
                services.AddSingleton<ModulesPageViewModel>();
                services.AddSingleton<ProcessListPage>();
                services.AddSingleton<ProcessListPageViewModel>();
                services.AddSingleton<ResourceUsagePage>();
                services.AddSingleton<ResourceUsagePageViewModel>();
                services.AddSingleton<WatsonsPage>();
                services.AddSingleton<WatsonPageViewModel>();
                services.AddSingleton<WinLogsPage>();
                services.AddSingleton<WinLogsPageViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsPageViewModel>();

                // Settings sub-pages and viewmodels.
                services.AddTransient<PreferencesViewModel>();
                services.AddTransient<PreferencesPage>();
                services.AddTransient<AdditionalToolsViewModel>();
                services.AddTransient<AdditionalToolsPage>();
                services.AddTransient<AdvancedSettingsViewModel>();
                services.AddTransient<AdvancedSettingsPage>();
                services.AddTransient<AboutViewModel>();
                services.AddTransient<AboutPage>();
            }).Build();
    }

    internal static bool IsFeatureEnabled()
    {
        var isEnabled = false;

        ApplicationData.Current.LocalSettings.Values.TryGetValue($"ExperimentalFeature_ProjectIronsidesExperiment", out var isEnabledObj);
        if (isEnabledObj is not null && isEnabledObj is string isEnabledValue)
        {
            isEnabled = isEnabledValue == "true";
        }
        else
        {
#if DEBUG
            // Override on debug builds to be enabled by default
            isEnabled = true;
#endif
        }

        return isEnabled;
    }

    internal static ITelemetry Logger => TelemetryFactory.Get<ITelemetry>();

    internal static void LogTimeTaken(string eventName, uint timeTakenMilliseconds, Guid? relatedActivityId = null) => Logger.LogTimeTaken(eventName, timeTakenMilliseconds, relatedActivityId);

    internal static void LogCritical(string eventName, bool isError = false, Guid? relatedActivityId = null) => Logger.LogCritical(eventName, isError, relatedActivityId);

    internal static void Log<T>(string eventName, LogLevel level, T data, Guid? relatedActivityId = null)
        where T : EventBase
    {
        Logger.Log<T>(eventName, level, data, relatedActivityId ?? null);
    }

    internal static void LogError<T>(string eventName, LogLevel level, T data, Guid? relatedActivityId = null)
        where T : EventBase
    {
        Logger.LogError<T>(eventName, level, data, relatedActivityId);
    }

    internal static void Log(string eventName, LogLevel level, Guid? relatedActivityId = null) => Logger.Log(eventName, level, new UsageEventData(), relatedActivityId);
}
