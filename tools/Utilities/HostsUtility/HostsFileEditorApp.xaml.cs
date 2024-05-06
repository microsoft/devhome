// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO.Abstractions;
using System.Threading;
using DevHome.Common.Contracts;
using DevHome.Common.Services;
using DevHome.HostsFileEditor.Helpers;
using DevHome.HostsFileEditor.TelemetryEvents;
using DevHome.HostsFileEditor.ViewModels;
using DevHome.HostsFileEditor.Views;
using DevHome.Telemetry;
using HostsUILib.Helpers;
using HostsUILib.Settings;
using HostsUILib.ViewModels;
using HostsUILib.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Serilog;
using static HostsUILib.Settings.IUserSettings;

namespace DevHome.HostsFileEditor;

public partial class HostsFileEditorApp : Application
{
    private Guid ActivityId { get; set; }

    private readonly Serilog.ILogger _log = Log.ForContext("SourceContext", nameof(HostsFileEditorApp));

    public IConfiguration Configuration { get; }

    public IHost Host { get; }

    public static T GetService<T>()
        where T : class
    {
        if ((HostsFileEditorApp.Current as HostsFileEditorApp)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within HostsFileEditorApp.xaml.cs.");
        }

        return service;
    }

    public HostsFileEditorApp()
    {
        ActivityId = Guid.NewGuid();
        TelemetryFactory.Get<ITelemetry>().Log("HostsFileEditorApp_HostsFileEditorApp", LogLevel.Measure, new HostsFileEditorTraceEvent(), ActivityId);

        DevHome.Common.Logging.SetupLogging("appsettings_hostsfileeditor.json", "HostsUtility");
        _log.Information("HostsFileEditorApp");

        this.InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder().UseContentRoot(AppContext.BaseDirectory).ConfigureServices((hostContext, services) =>
        {
            // Core Services
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IHostsService, HostsService>();

            services.AddSingleton<HostsUILib.Helpers.ILogger, LoggerWrapper>();
            services.AddSingleton<IUserSettings, UserSettings>();
            services.AddSingleton<IElevationHelper, ElevationHelper>();
            services.AddSingleton<OpenSettingsFunction>(() =>
            {
                settingsWindow = new HostsFileEditorSettingsWindow();
                settingsWindow.Activate();
            });

            // Views and ViewModels
            services.AddSingleton<MainViewModel, MainViewModel>();
            services.AddSingleton<HostsMainPage, HostsMainPage>();
            services.AddSingleton<HostsFileEditorSettingsViewModel, HostsFileEditorSettingsViewModel>();
            services.AddSingleton<HostsFileEditorSettingsWindow, HostsFileEditorSettingsWindow>();

            // Settings
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
        }).Build();

        var cleanupBackupThread = new Thread(() =>
        {
            // Delete old backups only if running elevated
            if (!GetService<IElevationHelper>().IsElevated)
            {
                return;
            }

            try
            {
                GetService<IHostsService>().CleanupBackup();
            }
            catch (Exception ex)
            {
                _log.Error("Failed to delete backup", ex);
            }
        });

        cleanupBackupThread.IsBackground = true;
        cleanupBackupThread.Start();

        TelemetryFactory.Get<ITelemetry>().Log("HostsFileEditorApp_HostsFileEditorApp_Initialized", LogLevel.Measure, new HostsFileEditorTraceEvent(), ActivityId);
        _log.Information("HostsFileEditorApp Initialized");
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        mainWindow = new HostsFileEditorMainWindow(ActivityId);
        mainWindow.Activate();

        TelemetryFactory.Get<DevHome.Telemetry.ITelemetry>().Log("HostsFileEditorApp_HostsFileEditorApp_Launched", LogLevel.Critical, new HostsFileEditorAppLaunchEvent(), null);
        _log.Information("HostsFileEditorApp Launched");
    }

    private Window mainWindow;
    private Window settingsWindow;
}
