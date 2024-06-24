// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Services;
using DevHome.Telemetry;
using DevHome.Utilities.TelemetryEvents;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.Utilities.ViewModels;

public partial class UtilityViewModel : ObservableObject
{
#nullable enable

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(UtilityViewModel));
    private readonly IExperimentationService? experimentationService;
    private readonly string? experimentalFeature;
    private readonly string _exeName;
#nullable disable

    public bool Visible
    {
        get
        {
            // Query if there is an experimental feature and return its enabled value
            if (experimentalFeature is not null)
            {
                var isExperimentalFeatureEnabled = experimentationService?.IsFeatureEnabled(experimentalFeature) ?? true;
                return isExperimentalFeatureEnabled;
            }

            return true;
        }
    }

    public string Title { get; set; }

    public string Description { get; set; }

    public string NavigateUri { get; set; }

    public string ImageSource { get; set; }

    public ICommand LaunchCommand { get; set; }

    public Visibility SupportsLaunchAsAdmin { get; set; }

    [ObservableProperty]
    private bool _launchAsAdmin;

    [ObservableProperty]
    private string _utilityAutomationId;

#nullable enable
    public UtilityViewModel(string exeName, IExperimentationService? experimentationService = null, string? experimentalFeature = null)
    {
        this._exeName = exeName;
        this.experimentationService = experimentationService;
        this.experimentalFeature = experimentalFeature;
        LaunchCommand = new RelayCommand(Launch);
        _log.Information($"UtilityViewModel created for Title: {Title}, exe: {exeName}");
    }
#nullable disable

    private void Launch()
    {
        var activityId = Guid.NewGuid();
        _log.Information($"Launching {_exeName}, as admin: {LaunchAsAdmin}");
        TelemetryFactory.Get<ITelemetry>().Log("Utilities_UtilitiesLaunchEvent", LogLevel.Critical, new UtilitiesLaunchEvent(activityId, Title, LaunchAsAdmin, UtilitiesLaunchEvent.Phase.Start));

        // We need to start the process with ShellExecute to run elevated
        var processStartInfo = new ProcessStartInfo
        {
            FileName = _exeName,
            UseShellExecute = true,

            Verb = LaunchAsAdmin ? "runas" : "open",
        };

        try
        {
            var process = Process.Start(processStartInfo);
            if (process is null)
            {
                _log.Error($"Failed to start process {_exeName}");
                TelemetryFactory.Get<ITelemetry>().Log("Utilities_UtilitiesLaunchEvent", LogLevel.Critical, new UtilitiesLaunchEvent(activityId, Title, LaunchAsAdmin, UtilitiesLaunchEvent.Phase.Error));
                throw new InvalidOperationException("Failed to start process");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to start process {_exeName}");
            TelemetryFactory.Get<ITelemetry>().Log("Utilities_UtilitiesLaunchEvent", LogLevel.Critical, new UtilitiesLaunchEvent(activityId, Title, LaunchAsAdmin, UtilitiesLaunchEvent.Phase.Error, ex.ToString()));
        }

        TelemetryFactory.Get<ITelemetry>().Log("Utilities_UtilitiesLaunchEvent", LogLevel.Critical, new UtilitiesLaunchEvent(activityId, Title, LaunchAsAdmin, UtilitiesLaunchEvent.Phase.Complete), null);
    }
}
