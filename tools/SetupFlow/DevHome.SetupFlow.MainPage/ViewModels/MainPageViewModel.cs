// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.AppManagement;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.SetupFlow.ConfigurationFile;
using DevHome.SetupFlow.DevDrive;
using DevHome.SetupFlow.DevDrive.Models;
using DevHome.SetupFlow.DevDrive.Utilities;
using DevHome.SetupFlow.RepoConfig;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Windows.System;

namespace DevHome.SetupFlow.MainPage.ViewModels;

/// <summary>
/// View model for the main page of the Setup Flow.
/// This page contains controls to start the setup flow with different
/// combinations of steps to perform. For example, only Configuration file,
/// or a full flow with Dev Volume, Clone Repos, and App Management.
/// </summary>
public partial class MainPageViewModel : SetupPageViewModelBase
{
    private readonly ILogger _logger;
    private readonly IHost _host;

    [ObservableProperty]
    private bool _showDevDriveItem;

    /// <summary>
    /// Event raised when the user elects to start the setup flow.
    /// The orchestrator for the whole flow subscribes to this event to handle
    /// all the work needed at that point.
    /// </summary>
    public event EventHandler<IList<ISetupTaskGroup>> StartSetupFlow;

    public MainPageViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        ILogger logger,
        IHost host)
        : base(stringResource, orchestrator)
    {
        _logger = logger;
        _host = host;

        IsNavigationBarVisible = false;
        IsStepPage = false;
        ShowDevDriveItem = DevDriveUtil.IsDevDriveFeatureEnabled;
    }

    /// <summary>
    /// Starts the setup flow including the pages for the given task groups.
    /// </summary>
    /// <param name="taskGroups">The task groups that will be included in this setup flow.</param>
    private void StartSetupFlowForTaskGroups(params ISetupTaskGroup[] taskGroups)
    {
        StartSetupFlow.Invoke(null, taskGroups);
    }

    /// <summary>
    /// Starts a full setup flow, with all the possible task groups.
    /// </summary>
    [RelayCommand]
    private void StartSetup()
    {
        StartSetupFlowForTaskGroups(
            _host.GetService<DevDriveTaskGroup>(),
            _host.GetService<RepoConfigTaskGroup>(),
            _host.GetService<AppManagementTaskGroup>());
    }

    /// <summary>
    /// Starts a setup flow that only includes repo config.
    /// </summary>
    [RelayCommand]
    private void StartRepoConfig()
    {
        StartSetupFlowForTaskGroups(
            _host.GetService<DevDriveTaskGroup>(),
            _host.GetService<RepoConfigTaskGroup>());
    }

    /// <summary>
    /// Starts a setup flow that only includes app management.
    /// </summary>
    [RelayCommand]
    private void StartAppManagement()
    {
        StartSetupFlowForTaskGroups(_host.GetService<AppManagementTaskGroup>());
    }

    /// <summary>
    /// Opens the Windows settings app and redirects the user to the disks and volumes page.
    /// </summary>
    [RelayCommand]
    private async void LaunchDisksAndVolumesSettingsPage()
    {
        // TODO: Add telemetry.
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
            StartSetupFlowForTaskGroups(configFileSetupFlow);
        }
    }

    [RelayCommand]
    private async Task BannerButtonAsync()
    {
        // TODO Update code with the "Learn more" button behavior
        await Launcher.LaunchUriAsync(new ("https://microsoft.com"));
    }
}
