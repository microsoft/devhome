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
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.Utilities.ViewModels;

public class UtilityViewModel : INotifyPropertyChanged
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

    private bool launchAsAdmin;

    public string Title { get; set; }

    public string Description { get; set; }

    public string NavigateUri { get; set; }

    public string ImageSource { get; set; }

    public ICommand LaunchCommand { get; set; }

    public Visibility LaunchAsAdminVisibility { get; set; }

    public bool LaunchAsAdmin
    {
        get => launchAsAdmin;

        set
        {
            if (launchAsAdmin != value)
            {
                launchAsAdmin = value;
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

#nullable enable
    public UtilityViewModel(string exeName, IExperimentationService? experimentationService = null, string? experimentalFeature = null)
    {
        this._exeName = exeName;
        this.experimentationService = experimentationService;
        this.experimentalFeature = experimentalFeature;
        LaunchCommand = new RelayCommand(Launch);
        _log.Information("UtilityViewModel created for Title: {Title}, exe: {ExeName}", Title, exeName);
    }
#nullable disable

    private void Launch()
    {
        _log.Information($"Launching {_exeName}, as admin: {launchAsAdmin}");

        // We need to start the process with ShellExecute to run elevated
        var processStartInfo = new ProcessStartInfo
        {
            FileName = _exeName,
            UseShellExecute = true,

            Verb = launchAsAdmin ? "runas" : "open",
        };

        try
        {
            var process = Process.Start(processStartInfo);
            if (process is null)
            {
                _log.Error("Failed to start process {ExeName}", exeName);
                throw new InvalidOperationException("Failed to start process");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to start process {ExeName}", _exeName);
        }

        TelemetryFactory.Get<DevHome.Telemetry.ITelemetry>().Log("Utilities_UtilitiesLaunchEvent", LogLevel.Critical, new UtilitiesLaunchEvent(Title, launchAsAdmin), null);
    }
}
