// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RegistryPreviewUILib;
using Serilog;
using WinUIEx;

namespace DevHome.RegistryPreview;

public sealed partial class RegistryPreviewMainWindow : WindowEx
{
    private const string APPNAME = "RegistryPreview";

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

        Title = APPNAME;
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/RegistryPreview/RegistryPreview.ico"));

        RegistryPreviewMainPageNugetMainPage = new RegistryPreviewMainPage(this, this.UpdateWindowTitle, RegistryPreviewApp.ActivatedFileName);

        TelemetryFactory.Get<ITelemetry>().Log("RegistryPreviewApp_RegistryPreviewMainWindow_Initialized", LogLevel.Measure, new EmptyEvent(), this.activityId);
        _log.Information("RegistryPreviewApp RegistryPreviewMainWindow Intialized");
    }

    private void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        MainGrid.Children.Add(RegistryPreviewMainPageNugetMainPage);
        Grid.SetRow(RegistryPreviewMainPageNugetMainPage, 1);

        TelemetryFactory.Get<ITelemetry>().Log("RegistryPreviewApp_RegistryPreviewMainWindow_GridLoaded", LogLevel.Measure, new EmptyEvent(), activityId);
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
            UtilityTitle = APPNAME;
        }
        else
        {
            var file = filenameTitle.Split('\\');
            if (file.Length > 0)
            {
                UtilityTitle = file[file.Length - 1] + " - " + APPNAME;
            }
            else
            {
                UtilityTitle = filenameTitle + " - " + APPNAME;
            }
        }

        Title = UtilityTitle;
    }
}
