// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.IfeoTool.TelemetryEvents;
using DevHome.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Serilog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;

namespace DevHome.IfeoTool;

public partial class IfeoToolApp : Application
{
    private readonly DispatcherQueue _dispatcher;

    internal static Guid ActivityId { get; set; }

    internal static string TargetAppName { get; set; } = string.Empty;

    internal static ILogger SLog { get; set; } = Serilog.Log.ForContext("SourceContext", nameof(IfeoToolApp));

    private Window? _mainWindow;

    public IHost Host { get; }

    public static T GetService<T>()
        where T : class
    {
        if ((IfeoToolApp.Current as IfeoToolApp)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within IfeoTool.xaml.cs.");
        }

        return service;
    }

    public IfeoToolApp()
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        ActivityId = Guid.NewGuid();

        Log("IfeoToolApp_IfeoToolApp", LogLevel.Measure);

        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder().UseContentRoot(AppContext.BaseDirectory).ConfigureServices((hostContext, services) =>
        {
            // Views and ViewModels
            services.AddSingleton<ImageOptionsControlViewModel, ImageOptionsControlViewModel>();
            services.AddSingleton<IfeoToolWindow, IfeoToolWindow>();
        }).Build();

        AppInstance.GetCurrent().Activated += IfeoToolApp_Activated;

        Log("IfeoToolApp_IfeoToolApp_Initialized", LogLevel.Measure);
    }

    private void IfeoToolApp_Activated(object? sender, AppActivationArguments e)
    {
        _dispatcher.TryEnqueue(DispatcherQueuePriority.High, () =>
        {
            if (_mainWindow != null)
            {
                var hwnd = (HWND)WindowNative.GetWindowHandle(_mainWindow);
                PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);

                // Activate is unreliable so use SetForegroundWindow
                PInvoke.SetForegroundWindow(hwnd);
            }

            Log<IfeoToolAppLaunchEvent>("IfeoToolApp_Activated", LogLevel.Critical, new IfeoToolAppLaunchEvent());
        });
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _mainWindow = new IfeoToolWindow();
        _mainWindow.Activate();

        Log<IfeoToolAppLaunchEvent>("IfeoToolApp_OnLaunched", LogLevel.Critical, new IfeoToolAppLaunchEvent());
    }

    internal static ITelemetry Logger => TelemetryFactory.Get<ITelemetry>();

    internal static void Log<T>(string eventName, LogLevel level, T data, Guid? relatedActivityId = null)
        where T : EventBase
    {
        Logger.Log<T>(eventName, level, data, relatedActivityId ?? IfeoToolApp.ActivityId);
        SLog.Information(eventName);
    }

    internal static void LogError<T>(string eventName, LogLevel level, T data, Guid? relatedActivityId = null)
        where T : EventBase
    {
        Logger.LogError<T>(eventName, level, data, relatedActivityId ?? IfeoToolApp.ActivityId);
        SLog.Information(eventName);
    }

    internal static void Log(string eventName, LogLevel level, Guid? relatedActivityId = null)
    {
        Logger.Log(eventName, level, new IfeoToolTraceEvent(), relatedActivityId ?? IfeoToolApp.ActivityId);
        SLog.Information(eventName);
    }
}
