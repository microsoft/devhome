// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Telemetry;
using DevHome.TelemetryEvents;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome;

public sealed partial class MainWindow : WinUIEx.WindowEx
{
    private readonly DateTime mainWindowCreated;

    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/DevHome.ico"));
        Content = null;
        Title = Application.Current.GetService<IAppInfoService>().GetAppNameLocalized();
        mainWindowCreated = DateTime.UtcNow;
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        Application.Current.GetService<IExtensionService>().SignalStopExtensionsAsync();
        TelemetryFactory.Get<ITelemetry>().Log("DevHome_MainWindow_Closed_Event", LogLevel.Critical, new DevHomeClosedEvent(mainWindowCreated));
        Log.Information("Terminating via MainWindow_Closed.");

        // WinUI bug is causing a crash on shutdown when FailFastOnErrors is set to true (#51773592).
        // Workaround by turning it off before shutdown.
        App.Current.DebugSettings.FailFastOnErrors = false;
    }
}
