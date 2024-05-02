// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Web;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Serilog;
using Windows.ApplicationModel.Activation;

namespace DevHome.RegistryPreview;

public partial class RegistryPreviewApp : Application
{
    private Guid ActivityId { get; set; }

    private readonly Serilog.ILogger _log = Log.ForContext("SourceContext", nameof(RegistryPreviewApp));

#pragma warning disable CA2211 // Non-constant fields should not be visible
#pragma warning disable SA1401 // Fields should be private
    public static string ActivatedFileName;
#pragma warning restore SA1401 // Fields should be private
#pragma warning restore CA2211 // Non-constant fields should not be visible

    public RegistryPreviewApp()
    {
        DevHome.Common.Logging.SetupLogging("appsettings_registrypreview.json", "RegistryPreview");

        ActivityId = Guid.NewGuid();
        TelemetryFactory.Get<ITelemetry>().Log("RegistryPreviewApp_RegistryPreviewApp", LogLevel.Measure, new EmptyEvent(), ActivityId);

        this.InitializeComponent();

        TelemetryFactory.Get<ITelemetry>().Log("RegistryPreviewApp_RegistryPreviewApp_Initialized", LogLevel.Measure, new EmptyEvent(), ActivityId);
        _log.Information("RegistryPreviewApp Initialized");
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        window = new RegistryPreviewMainWindow(ActivityId);
        window.Activate();

        TelemetryFactory.Get<ITelemetry>().Log("RegistryPreviewApp_RegistryPreviewApp_Launched", LogLevel.Measure, new EmptyEvent(), ActivityId);
        _log.Information("RegistryPreviewApp Launched");

        AppActivationArguments activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        if (activatedArgs.Kind == ExtendedActivationKind.File)
        {
            ActivatedFileName = string.Empty;
            if (activatedArgs.Data != null)
            {
                IFileActivatedEventArgs eventArgs = (IFileActivatedEventArgs)activatedArgs.Data;
                if (eventArgs.Files.Count > 0)
                {
                    ActivatedFileName = eventArgs.Files[0].Path;
                }
            }
        }
        else if (activatedArgs.Kind == ExtendedActivationKind.Protocol)
        {
            // When the app is the default handler for REG files and the filename has non-ASCII characters, the app gets activated by Protocol
            ActivatedFileName = string.Empty;
            if (activatedArgs.Data != null)
            {
                IProtocolActivatedEventArgs eventArgs = (IProtocolActivatedEventArgs)activatedArgs.Data;
                if (eventArgs.Uri.AbsoluteUri.Length > 0)
                {
                    ActivatedFileName = eventArgs.Uri.Query.Replace("?ContractId=Windows.File&Verb=open&File=", string.Empty);
                    ActivatedFileName = HttpUtility.UrlDecode(ActivatedFileName);
                }
            }
        }
        else
        {
            var cmdArgs = Environment.GetCommandLineArgs();
            if (cmdArgs == null)
            {
                ActivatedFileName = string.Empty;
            }
            else if (cmdArgs.Length == 2)
            {
                ActivatedFileName = cmdArgs[1];
            }
            else
            {
                ActivatedFileName = string.Empty;
            }
        }
    }

    private Window window;
}
