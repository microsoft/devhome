// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;
using DevHome.SetupFlow.Utilities;
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
    private readonly DispatcherQueue _dispatcherQueue;

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

    public MainPageViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        IWindowsPackageManager wpm,
        IHost host)
        : base(stringResource, orchestrator)
    {
        _host = host;
        _wpm = wpm;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

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
        ShowBanner = false;
    }

    /// <summary>
    /// Starts the setup flow including the pages for the given task groups.
    /// </summary>
    /// <param name="flowTitle">Title to show in the flow; will use the SetupShell.Title property if empty</param>
    /// <param name="taskGroups">The task groups that will be included in this setup flow.</param>
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
        Log.Logger?.ReportInfo(Log.Component.MainPage, "Starting end-to-end setup");

        var taskGroups = new List<ISetupTaskGroup>
        {
            _host.GetService<DevDriveTaskGroup>(),
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

        StartSetupFlowForTaskGroups(flowTitle, taskGroups.ToArray());
    }

    /// <summary>
    /// Starts a setup flow that only includes repo config.
    /// </summary>
    [RelayCommand]
    private void StartRepoConfig(string flowTitle)
    {
        Log.Logger?.ReportInfo(Log.Component.MainPage, "Starting flow for repo config");
        StartSetupFlowForTaskGroups(
            flowTitle,
            _host.GetService<DevDriveTaskGroup>(),
            _host.GetService<RepoConfigTaskGroup>());
    }

    /// <summary>
    /// Starts a setup flow that only includes app management.
    /// </summary>
    [RelayCommand]
    private void StartAppManagement(string flowTitle)
    {
        Log.Logger?.ReportInfo(Log.Component.MainPage, "Starting flow for app management");
        StartSetupFlowForTaskGroups(flowTitle,  _host.GetService<AppManagementTaskGroup>());
    }

    /// <summary>
    /// Opens the Windows settings app and redirects the user to the disks and volumes page.
    /// </summary>
    [RelayCommand]
    private async void LaunchDisksAndVolumesSettingsPage()
    {
        // TODO: Add telemetry.
        Log.Logger?.ReportInfo(Log.Component.MainPage, "Launching settings on Disks and Volumes page");
        await Launcher.LaunchUriAsync(new Uri("ms-settings:disksandvolumes"));
    }

    /// <summary>
    /// Starts a setup flow that only includes configuration file.
    /// </summary>
    [RelayCommand]
    private async Task StartConfigurationFileAsync()
    {
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
        // TODO Update code with the "Learn more" button behavior
        await Launcher.LaunchUriAsync(new ("https://microsoft.com"));
    }

    [RelayCommand]
    private void HideAppInstallUpdateNotification()
    {
        Log.Logger?.ReportInfo(Log.Component.MainPage, "Hiding AppInstaller update notification");
        ShowAppInstallerUpdateNotification = false;
    }

    [RelayCommand]
    private async Task UpdateAppInstallerAsync()
    {
        HideAppInstallUpdateNotification();

        if (await _wpm.StartAppInstallerUpdateAsync())
        {
            Log.Logger?.ReportInfo(Log.Component.MainPage, "AppInstaller update started");
        }
        else
        {
            Log.Logger?.ReportInfo(Log.Component.MainPage, "AppInstaller update did not start");
        }
    }
}
