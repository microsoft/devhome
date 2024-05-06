// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.RegistryPreview.TelemetryEvents;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.RegistryPreview;

public partial class RegistryPreviewApp : Application
{
    private Guid ActivityId { get; set; }

    private readonly Serilog.ILogger _log = Log.ForContext("SourceContext", nameof(RegistryPreviewApp));

    public RegistryPreviewApp()
    {
        DevHome.Common.Logging.SetupLogging("appsettings_registrypreview.json", "RegistryPreview");

        ActivityId = Guid.NewGuid();
        TelemetryFactory.Get<ITelemetry>().Log("RegistryPreviewApp_RegistryPreviewApp", LogLevel.Measure, new RegistryPreviewTraceEvent(), ActivityId);

        this.InitializeComponent();

        TelemetryFactory.Get<ITelemetry>().Log("RegistryPreviewApp_RegistryPreviewApp_Initialized", LogLevel.Measure, new RegistryPreviewTraceEvent(), ActivityId);
        _log.Information("RegistryPreviewApp Initialized");
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        window = new RegistryPreviewMainWindow(ActivityId);
        window.Activate();

        TelemetryFactory.Get<ITelemetry>().Log("RegistryPreviewApp_RegistryPreviewApp_Launched", LogLevel.Critical, new RegistryPreviewAppLaunchEvent(), ActivityId);
        _log.Information("RegistryPreviewApp Launched");
    }

    private Window window;
}
