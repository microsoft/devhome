// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.TelemetryEvents;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.TelemetryEvents;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;
using DevHome.SetupFlow.Utilities;
using DevHome.Telemetry;
using DevHome.TelemetryEvents;
using Microsoft.Extensions.Hosting;
using Windows.System;

namespace DevHome.SetupFlow.ViewModels;

/// <summary>
/// View model for the main page of the Setup Flow.
/// This page contains controls to start the setup flow with different
/// combinations of steps to perform. For example, only Configuration file,
/// or a full flow with Dev Volume, Clone Repos, and App Management.
/// </summary>
public partial class MainPageViewModel : SetupPageViewModelBase
{
    private readonly IHost _host;
    private readonly IWindowsPackageManager _wpm;

    [ObservableProperty]
    private bool _showBanner = true;

    [ObservableProperty]
    private bool _showDevDriveItem;

    [ObservableProperty]
    private bool _showPackageInstallerItem;

    [ObservableProperty]
    private bool _showAppInstallerUpdateNotification;

    /// <summary>
    /// Event raised when the user elects to start the setup flow.
    /// The orchestrator for the whole flow subscribes to this event to handle
    /// all the work needed at that point.
    /// </summary>
    public event EventHandler<(string, IList<ISetupTaskGroup>)> StartSetupFlow;

    public string AppInstallerUpdateAvailableTitle => StringResource.GetLocalized(StringResourceKey.AppInstallerUpdateAvailableTitle);

    public string AppInstallerUpdateAvailableMessage => StringResource.GetLocalized(StringResourceKey.AppInstallerUpdateAvailableMessage);

    public string AppInstallerUpdateAvailableUpdateButton => StringResource.GetLocalized(StringResourceKey.AppInstallerUpdateAvailableUpdateButton);

    public string AppInstallerUpdateAvailableCancelButton => StringResource.GetLocalized(StringResourceKey.AppInstallerUpdateAvailableCancelButton);

    public MainPageViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        IWindowsPackageManager wpm,
        IHost host)
        : base(stringResource, orchestrator)
    {
        _host = host;
        _wpm = wpm;

        IsNavigationBarVisible = false;
        IsStepPage = false;
        ShowDevDriveItem = DevDriveUtil.IsDevDriveFeatureEnabled;
    }

    protected async override Task OnFirstNavigateToAsync()
    {
        // If IsCOMServerAvailable is still being (lazily) evaluated form a
        // previous call, then await until the thread is unblocked and the
        // already computed value is returned.
        ShowPackageInstallerItem = await Task.Run(() => _wpm.IsCOMServerAvailable());
        if (ShowPackageInstallerItem)
        {
            Log.Logger?.ReportInfo($"{nameof(WindowsPackageManager)} COM Server is available. Showing package install item");
            ShowAppInstallerUpdateNotification = await _wpm.IsAppInstallerUpdateAvailableAsync();
        }
        else
        {
            Log.Logger?.ReportWarn($"{nameof(WindowsPackageManager)} COM Server is not available. Package install item is hidden.");
        }
    }

    [RelayCommand]
    private void HideBanner()
    {
        TelemetryFactory.Get<ITelemetry>().LogMeasure("MainPage_HideLearnMoreBanner_Event");
        ShowBanner = false;
    }

    /// <summary>
    /// Starts the setup flow including the pages for the given task groups.
    /// </summary>
    /// <param name="flowTitle">Title to show in the flow; will use the SetupShell.Title property if empty</param>
    /// <param name="taskGroups">The task groups that will be included in this setup flow.</param>
    /// <remarks>
    /// Note that the order of the task groups here will influence the order of the pages in
    /// the flow and the tabs in the review page.
    /// </remarks>
    private void StartSetupFlowForTaskGroups(string flowTitle, params ISetupTaskGroup[] taskGroups)
    {
        StartSetupFlow.Invoke(null, (flowTitle, taskGroups));
    }

    private void StartSetupFlowForTaskGroups(params ISetupTaskGroup[] taskGroups)
    {
        StartSetupFlowForTaskGroups(string.Empty, taskGroups);
    }

    /// <summary>
    /// Starts a full setup flow, with all the possible task groups.
    /// </summary>
    [RelayCommand]
    private void StartSetup(string flowTitle)
    {
        TelemetryFactory.Get<ITelemetry>().Log("MainPage_StartFlow_Event", LogLevel.Measure, new StartFlowEvent(flowTitle));
        Log.Logger?.ReportInfo(Log.Component.MainPage, "Starting end-to-end setup");

        var taskGroups = new List<ISetupTaskGroup>
        {
            _host.GetService<RepoConfigTaskGroup>(),
        };

        if (ShowPackageInstallerItem)
        {
            taskGroups.Add(_host.GetService<AppManagementTaskGroup>());
        }
        else
        {
            Log.Logger?.ReportInfo(Log.Component.MainPage, $"Skipping {nameof(AppManagementTaskGroup)} because COM server is not available");
        }

        taskGroups.Add(_host.GetService<DevDriveTaskGroup>());

        StartSetupFlowForTaskGroups(flowTitle, taskGroups.ToArray());
    }

    /// <summary>
    /// Starts a setup flow that only includes repo config.
    /// </summary>
    [RelayCommand]
    private void StartRepoConfig(string flowTitle)
    {
        TelemetryFactory.Get<ITelemetry>().Log("MainPage_StartFlow_Event", LogLevel.Measure, new StartFlowEvent(flowTitle));
        Log.Logger?.ReportInfo(Log.Component.MainPage, "Starting flow for repo config");
        StartSetupFlowForTaskGroups(
            flowTitle,
            _host.GetService<RepoConfigTaskGroup>(),
            _host.GetService<DevDriveTaskGroup>());
    }

    /// <summary>
    /// Starts a setup flow that only includes app management.
    /// </summary>
    [RelayCommand]
    private void StartAppManagement(string flowTitle)
    {
        TelemetryFactory.Get<ITelemetry>().Log("MainPage_StartFlow_Event", LogLevel.Measure, new StartFlowEvent(flowTitle));
        Log.Logger?.ReportInfo(Log.Component.MainPage, "Starting flow for app management");
        StartSetupFlowForTaskGroups(flowTitle,  _host.GetService<AppManagementTaskGroup>());
    }

    /// <summary>
    /// Opens the Windows settings app and redirects the user to the disks and volumes page.
    /// </summary>
    [RelayCommand]
    private async void LaunchDisksAndVolumesSettingsPage()
    {
        Log.Logger?.ReportInfo(Log.Component.MainPage, "Launching settings on Disks and Volumes page");
        TelemetryFactory.Get<ITelemetry>().Log(
            "LaunchDisksAndVolumesSettingsPageTriggered",
            LogLevel.Measure,
            new DisksAndVolumesSettingsPageTriggeredEvent(source: "MainPageView"));
        await Launcher.LaunchUriAsync(new Uri("ms-settings:disksandvolumes"));
    }

    /// <summary>
    /// Starts a setup flow that only includes configuration file.
    /// </summary>
    [RelayCommand]
    private async Task StartConfigurationFileAsync()
    {
        TelemetryFactory.Get<ITelemetry>().Log("MainPage_StartFlow_Event", LogLevel.Measure, new StartFlowEvent("ConfigurationFile"));
        Log.Logger?.ReportInfo(Log.Component.MainPage, "Launching settings on Disks and Volumes page");
        var configFileSetupFlow = _host.GetService<ConfigurationFileTaskGroup>();
        if (await configFileSetupFlow.PickConfigurationFileAsync())
        {
            Log.Logger?.ReportInfo(Log.Component.MainPage, "Starting flow for Configuration file");
            StartSetupFlowForTaskGroups(configFileSetupFlow);
        }
    }

    [RelayCommand]
    private async Task BannerButtonAsync()
    {
        await Launcher.LaunchUriAsync(new ("https://go.microsoft.com/fwlink/?linkid=2235076"));
    }

    [RelayCommand]
    private void HideAppInstallerUpdateNotification()
    {
        Log.Logger?.ReportInfo(Log.Component.MainPage, "Hiding AppInstaller update notification");
        ShowAppInstallerUpdateNotification = false;
    }

    [RelayCommand]
    private async Task UpdateAppInstallerAsync()
    {
        // Hide notification and attempt the update in the background.
        // Update progress should be reflected in the store app (if successful)
        HideAppInstallerUpdateNotification();
        if (await _wpm.StartAppInstallerUpdateAsync())
        {
            Log.Logger?.ReportInfo(Log.Component.MainPage, "AppInstaller update started");
        }
        else
        {
            Log.Logger?.ReportWarn(Log.Component.MainPage, "AppInstaller update did not start");
        }
    }
}
