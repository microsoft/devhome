// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using DevHome.Common.Services;
using DevHome.HostsFileEditor.Helpers;
using DevHome.HostsFileEditor.TelemetryEvents;
using DevHome.Telemetry;
using HostsUILib.Helpers;
using HostsUILib.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using WinUIEx;

namespace DevHome.HostsFileEditor;

public sealed partial class HostsFileEditorMainWindow : WindowEx
{
    private HostsMainPage HostsNugetMainPage { get; }

    private string UtilityTitle { get; set; }

    private readonly Guid activityId;

    private readonly Serilog.ILogger _log = Log.ForContext("SourceContext", nameof(HostsFileEditorApp));

    public HostsFileEditorMainWindow(Guid activityId)
    {
        this.activityId = activityId;
        this.InitializeComponent();

        Activated += MainWindow_Activated;

        ExtendsContentIntoTitleBar = true;

        var stringResource = new StringResource("PowerToys.HostsUILib.pri", "PowerToys.HostsUILib/Resources");
        var title = HostsFileEditorApp.GetService<IElevationHelper>().IsElevated ? stringResource.GetLocalized("WindowAdminTitle") : stringResource.GetLocalized("WindowTitle");
        UtilityTitle = title;

        Title = UtilityTitle;
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/HostsUILib/Hosts.ico"));

        HostsNugetMainPage = HostsFileEditorApp.GetService<HostsMainPage>();

        TelemetryFactory.Get<ITelemetry>().Log("HostsFileEditorApp_HostsFileEditorMainWindow_Initialized", LogLevel.Measure, new HostsFileEditorTraceEvent(), this.activityId);
        _log.Information("HostsFileEditorApp HostsFileEditorMainWindow Intialized");
    }

    private void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        MainGrid.Children.Add(HostsNugetMainPage);
        Grid.SetRow(HostsNugetMainPage, 1);

        TelemetryFactory.Get<ITelemetry>().Log("HostsFileEditorApp_HostsFileEditorMainWindow_GridLoaded", LogLevel.Measure, new HostsFileEditorTraceEvent(), activityId);
        _log.Information("HostsFileEditorApp HostsFileEditorMainWindow Grid loaded");
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        AppTitleBar.IsActive = args.WindowActivationState != WindowActivationState.Deactivated;
    }
}
