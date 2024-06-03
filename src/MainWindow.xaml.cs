// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Telemetry;
using DevHome.TelemetryEvents;
using Microsoft.UI.Xaml;

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
    }
}
