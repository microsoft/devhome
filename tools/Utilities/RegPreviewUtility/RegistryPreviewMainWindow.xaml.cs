// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using DevHome.Common.Services;
using DevHome.RegistryPreview.TelemetryEvents;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RegistryPreviewUILib;
using Serilog;
using WinUIEx;

namespace DevHome.RegistryPreview;

public sealed partial class RegistryPreviewMainWindow : WindowEx
{
    private string AppName { get; set; }

    private string UtilityTitle { get; set; }

    private RegistryPreviewMainPage RegistryPreviewMainPageNugetMainPage { get; }

    private readonly Guid activityId;

    private readonly Serilog.ILogger _log = Log.ForContext("SourceContext", nameof(RegistryPreviewApp));

    public RegistryPreviewMainWindow(Guid activityId)
    {
        this.activityId = activityId;
        this.InitializeComponent();

        Activated += MainWindow_Activated;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        var stringResource = new StringResource(Path.Combine(AppContext.BaseDirectory, "..\\DevHome\\DevHome.RegistryPreview.pri"), "Resources");
        AppName = stringResource.GetLocalized("RegistryPreviewAppDisplayName");

        Title = AppName;
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/RegistryPreview/RegistryPreview.ico"));

        RegistryPreviewMainPageNugetMainPage = new RegistryPreviewMainPage(this, this.UpdateWindowTitle, string.Empty);

        TelemetryFactory.Get<ITelemetry>().Log("RegistryPreviewApp_RegistryPreviewMainWindow_Initialized", LogLevel.Measure, new RegistryPreviewTraceEvent(), this.activityId);
        _log.Information("RegistryPreviewApp RegistryPreviewMainWindow Intialized");
    }

    private void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        MainGrid.Children.Add(RegistryPreviewMainPageNugetMainPage);
        Grid.SetRow(RegistryPreviewMainPageNugetMainPage, 1);

        TelemetryFactory.Get<ITelemetry>().Log("RegistryPreviewApp_RegistryPreviewMainWindow_GridLoaded", LogLevel.Measure, new RegistryPreviewTraceEvent(), activityId);
        _log.Information("RegistryPreviewApp RegistryPreviewMainWindow Grid loaded");
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        AppTitleBar.IsActive = args.WindowActivationState != WindowActivationState.Deactivated;
    }

    public void UpdateWindowTitle(string filenameTitle)
    {
        if (string.IsNullOrEmpty(filenameTitle))
        {
            UtilityTitle = AppName;
        }
        else
        {
            var file = filenameTitle.Split('\\');
            if (file.Length > 0)
            {
                UtilityTitle = file[file.Length - 1] + " - " + AppName;
            }
            else
            {
                UtilityTitle = filenameTitle + " - " + AppName;
            }
        }

        Title = UtilityTitle;
        AppTitleBar.Title = UtilityTitle;
    }
}
