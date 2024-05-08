// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO.Abstractions;
using DevHome.EnvironmentVariables.Helpers;
using DevHome.EnvironmentVariables.TelemetryEvents;
using DevHome.Telemetry;
using EnvironmentVariablesUILib;
using EnvironmentVariablesUILib.Helpers;
using EnvironmentVariablesUILib.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.EnvironmentVariables;

public partial class EnvironmentVariablesApp : Application
{
    private Guid ActivityId { get; set; }

    private readonly Serilog.ILogger _log = Log.ForContext("SourceContext", nameof(EnvironmentVariablesApp));

    public IHost Host { get; }

    public static T GetService<T>()
        where T : class
    {
        if ((EnvironmentVariablesApp.Current as EnvironmentVariablesApp)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within EnvironmentVariablesApp.xaml.cs.");
        }

        return service;
    }

    public EnvironmentVariablesApp()
    {
        DevHome.Common.Logging.SetupLogging("appsettings_environmentvariables.json", "EnvironmentVariables");

        ActivityId = Guid.NewGuid();
        TelemetryFactory.Get<DevHome.Telemetry.ITelemetry>().Log("EnvironmentVariablesApp_EnvironmentVariablesApp", LogLevel.Measure, new EnvironmentVariablesTraceEvent(), ActivityId);

        this.InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder().UseContentRoot(AppContext.BaseDirectory).ConfigureServices((context, services) =>
        {
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IElevationHelper, ElevationHelper>();
            services.AddSingleton<IEnvironmentVariablesService, EnvironmentVariablesService>();
            services.AddSingleton<EnvironmentVariablesUILib.Helpers.ILogger, LoggerWrapper>();
            services.AddSingleton<EnvironmentVariablesUILib.Telemetry.ITelemetry, TelemetryWrapper>();

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<EnvironmentVariablesMainPage>();
        }).Build();

        TelemetryFactory.Get<DevHome.Telemetry.ITelemetry>().Log("EnvironmentVariablesApp_EnvironmentVariablesApp_Initialized", LogLevel.Measure, new EnvironmentVariablesTraceEvent(), ActivityId);
        _log.Information("EnvironmentVariablesApp Initialized");
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        window = new EnvironmentVariablesMainWindow(ActivityId);
        window.Activate();

        TelemetryFactory.Get<DevHome.Telemetry.ITelemetry>().Log("EnvironmentVariablesApp_EnvironmentVariablesApp_Launched", LogLevel.Critical, new EnvironmentVariablesAppLaunchEvent(), ActivityId);
        _log.Information("EnvironmentVariablesApp Launched");
    }

    private Window window;
}
