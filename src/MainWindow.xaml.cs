// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.InteropServices;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Helpers;
using DevHome.Logging;
using DevHome.Telemetry;
using DevHome.TelemetryEvents;
using Microsoft.UI.Xaml;

namespace DevHome;

public sealed partial class MainWindow : WindowEx
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

    private void MainWindow_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        Application.Current.GetService<IExtensionService>().SignalStopExtensionsAsync();
        TelemetryFactory.Get<ITelemetry>().Log("DevHome_MainWindow_Closed_Event", LogLevel.Critical, new DevHomeClosedEvent(mainWindowCreated));
    }

    internal const int AsfwAny = -1;

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int id);

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (!AllowSetForegroundWindow(AsfwAny))
        {
            Log.Logger()?.ReportInfo($"Failed to Allow Set Foreground Window");
        }
    }
}
