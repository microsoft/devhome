// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Services;
using DevHome.Telemetry;
using DevHome.Utilities.TelemetryEvents;
using Serilog;

namespace DevHome.Utilities.ViewModels;

public class UtilityViewModel : INotifyPropertyChanged
{
#nullable enable

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(UtilityViewModel));
    private readonly IExperimentationService? experimentationService;
    private readonly string? experimentalFeature;
    private readonly string exeName;
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

    public ICommand LaunchAsAdminCommand { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

#nullable enable
    public UtilityViewModel(string exeName, IExperimentationService? experimentationService = null, string? experimentalFeature = null)
    {
        this.exeName = exeName;
        this.experimentationService = experimentationService;
        this.experimentalFeature = experimentalFeature;
        LaunchCommand = new RelayCommand(Launch);
        LaunchAsAdminCommand = new RelayCommand(LaunchAsAdmin);
        _log.Information("UtilityViewModel created for Title: {Title}, exe: {ExeName}", Title, exeName);
    }
#nullable disable

    private void Launch()
    {
        LaunchInternal(false);
    }

    private void LaunchAsAdmin()
    {
        LaunchInternal(true);
    }

    private void LaunchInternal(bool runAsAdmin)
    {
        _log.Information("Launching {ExeName}, as admin: {RunAsAdmin}", exeName, runAsAdmin);

        // We need to start the process with ShellExecute to run elevated
        var processStartInfo = new ProcessStartInfo
        {
            FileName = exeName,
            UseShellExecute = true,

            Verb = runAsAdmin ? "runas" : "open",
        };

        var process = Process.Start(processStartInfo);
        if (process is null)
        {
            _log.Error("Failed to start process {ExeName}", exeName);
            throw new InvalidOperationException("Failed to start process");
        }

        TelemetryFactory.Get<DevHome.Telemetry.ITelemetry>().Log("Utilities_UtilitiesLaunchEvent", LogLevel.Critical, new UtilitiesLaunchEvent(Title, runAsAdmin), null);
    }
}
