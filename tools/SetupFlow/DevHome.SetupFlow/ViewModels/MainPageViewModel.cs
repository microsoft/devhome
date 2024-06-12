// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;
using DevHome.SetupFlow.Utilities;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Serilog;
using Windows.Storage;
using Windows.System;

namespace DevHome.SetupFlow.ViewModels;

/// <summary>
/// View model for the main page of the Setup Flow.
/// This page contains controls to start the setup flow with different
/// combinations of steps to perform. For example, only Configuration file,
/// or a full flow with Dev Volume, Clone Repos, and App Management.
/// </summary>
public partial class MainPageViewModel : SetupPageViewModelBase, IDisposable
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(MainPageViewModel));

    private const string QuickstartPlaygroundFlowFeatureName = "QuickstartPlayground";

    private readonly IHost _host;
    private readonly IWindowsPackageManager _wpm;
    private readonly IDesiredStateConfiguration _dsc;
    private readonly IExperimentationService _experimentationService;

    public MainPageBannerViewModel BannerViewModel { get; }

    [ObservableProperty]
    private bool _showDevDriveItem;

    [ObservableProperty]
    private bool _enablePackageInstallerItem;

    [ObservableProperty]
    private bool _enableConfigurationFileItem;

    [ObservableProperty]
    private bool _showAppInstallerUpdateNotification;

    [ObservableProperty]
    private bool _enableQuickstartPlayground;

    private bool _disposedValue;

    public string MainPageEnvironmentSetupGroupName => StringResource.GetLocalized(StringResourceKey.MainPageEnvironmentSetupGroup);

    public string MainPageQuickStepsGroupName => StringResource.GetLocalized(StringResourceKey.MainPageQuickConfigurationGroup);

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
        IDesiredStateConfiguration dsc,
        IHost host,
        MainPageBannerViewModel bannerViewModel,
        IExperimentationService experimentationService)
        : base(stringResource, orchestrator)
    {
        _host = host;
        _wpm = wpm;
        _dsc = dsc;
        _experimentationService = experimentationService;

        IsNavigationBarVisible = false;
        IsStepPage = false;
        ShowDevDriveItem = DevDriveUtil.IsDevDriveFeatureEnabled;

        BannerViewModel = bannerViewModel;

        // If the feature is turned on, it doesn't show up in the configuration section (toggling it off and on again fixes it)
        // It's because this is constructed after ExperimentalFeaturesViewModel, so the handler isn't added yet.
        _host.GetService<IExperimentationService>().ExperimentalFeatures.FirstOrDefault(f => string.Equals(f.Id, QuickstartPlaygroundFlowFeatureName, StringComparison.Ordinal))!.PropertyChanged += ExperimentalFeaturesViewModel_PropertyChanged;

        // Hack around this by setting the property explicitly based on the state of the feature.
        EnableQuickstartPlayground = _host.GetService<IExperimentationService>().ExperimentalFeatures.FirstOrDefault(f => string.Equals(f.Id, QuickstartPlaygroundFlowFeatureName, StringComparison.Ordinal))!.IsEnabled;
    }

    // Create a PropertyChanged handler that we will add to the ExperimentalFeaturesViewModel
    // to update the EnableQuickstartPlayground property when the feature is enabled/disabled.
    private void ExperimentalFeaturesViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ExperimentalFeature.IsEnabled))
        {
            EnableQuickstartPlayground = _host.GetService<IExperimentationService>().ExperimentalFeatures.FirstOrDefault(f => string.Equals(f.Id, QuickstartPlaygroundFlowFeatureName, StringComparison.Ordinal))!.IsEnabled;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                var experimentationService = _host.GetService<IExperimentationService>();
                if (experimentationService != null)
                {
                    experimentationService.ExperimentalFeatures.FirstOrDefault(f => string.Equals(f.Id, QuickstartPlaygroundFlowFeatureName, StringComparison.Ordinal))!.PropertyChanged -= ExperimentalFeaturesViewModel_PropertyChanged;
                }
            }

            _disposedValue = true;
        }
    }

    // Disconnect event handler when the view model is disposed.
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async Task StartConfigurationFileAsync(StorageFile file)
    {
        _log.Information("Launching configuration file flow");
        var configFileSetupFlow = _host.GetService<ConfigurationFileTaskGroup>();
        if (await configFileSetupFlow.LoadFromLocalFileAsync(file))
        {
            _log.Information("Started flow from file activation");
            StartSetupFlowForTaskGroups(null, "ConfigurationFile", configFileSetupFlow);
        }
    }

    internal void StartAppManagementFlow(string query = null)
    {
        _log.Information("Launching app management flow");
        var appManagementSetupFlow = _host.GetService<AppManagementTaskGroup>();
        StartSetupFlowForTaskGroups(null, "App Activation URI", appManagementSetupFlow);
        appManagementSetupFlow.HandleSearchQuery(query);
    }

    protected async override Task OnFirstNavigateToAsync()
    {
        if (await ValidateAppInstallerAsync())
        {
            _log.Information($"{nameof(WindowsPackageManager)} COM Server is available. Showing package install item");
            ShowAppInstallerUpdateNotification = await _wpm.IsUpdateAvailableAsync();
        }
        else
        {
            _log.Warning($"{nameof(WindowsPackageManager)} COM Server is not available. Package install item is hidden.");
        }
    }

    protected async override Task OnEachNavigateToAsync()
    {
        await ValidateAppInstallerAsync();
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
    private void StartSetupFlowForTaskGroups(string flowTitle, string flowNameForTelemetry, params ISetupTaskGroup[] taskGroups)
    {
        StartSetupFlow.Invoke(null, (flowTitle, taskGroups));

        // Report this after setting the flow pages as that will set an ActivityId
        // we can later use to correlate with the flow termination.
        _log.Information($"Starting setup flow with ActivityId={Orchestrator.ActivityId}");
        TelemetryFactory.Get<ITelemetry>().Log(
            "MainPage_StartFlow_Event",
            LogLevel.Critical,
            new StartFlowEvent(flowNameForTelemetry),
            relatedActivityId: Orchestrator.ActivityId);
    }

    /// <summary>
    /// Starts a full setup flow, with all the possible task groups.
    /// </summary>
    [RelayCommand]
    private void StartSetup(string flowTitle)
    {
        _log.Information("Starting end-to-end setup");
        StartSetupFlowForTaskGroups(
            flowTitle,
            "EndToEnd",
            _host.GetService<RepoConfigTaskGroup>(),
            _host.GetService<AppManagementTaskGroup>(),
            _host.GetService<DevDriveTaskGroup>());
    }

    /// <summary>
    /// Starts the setup target flow for remote machines.
    /// </summary>
    [RelayCommand]
    private void StartSetupForTargetEnvironment(string flowTitle)
    {
        _log.Information("Starting setup for target environment");
        StartSetupFlowForTaskGroups(
            flowTitle,
            "SetupTargetEnvironment",
            _host.GetService<SetupTargetTaskGroup>(),
            _host.GetService<RepoConfigTaskGroup>(),
            _host.GetService<AppManagementTaskGroup>());
    }

    /// <summary>
    /// Starts a setup flow that only includes repo config.
    /// </summary>
    [RelayCommand]
    private void StartRepoConfig(string flowTitle)
    {
        _log.Information("Starting flow for repo config");
        StartSetupFlowForTaskGroups(
            flowTitle,
            "RepoConfig",
            _host.GetService<RepoConfigTaskGroup>(),
            _host.GetService<DevDriveTaskGroup>());
    }

    [RelayCommand]
    private void StartQuickstart(string flowTitle)
    {
        _log.Information("Starting flow for developer quickstart playground");
        StartSetupFlowForTaskGroups(flowTitle, "DeveloperQuickstartPlayground", _host.GetService<DeveloperQuickstartTaskGroup>());
    }

    /// <summary>
    /// Starts the create environment flow.
    /// </summary>
    [RelayCommand]
    public void StartCreateEnvironment(string flowTitle)
    {
        StartCreateEnvironmentWithTelemetry(flowTitle, "StartCreationFlow", "Machine Configuration");
    }

    /// <summary>
    /// Starts the create environment flow and logs that the create environment button has been clicked. This
    /// can be generalized in the future so other flow can utilize it as well.
    /// </summary>
    public void StartCreateEnvironmentWithTelemetry(string flowTitle, string navigationAction, string originPage)
    {
        _log.Information("Starting flow for environment creation");
        StartSetupFlowForTaskGroups(
            flowTitle,
            "CreateEnvironment",
            _host.GetService<SelectEnvironmentProviderTaskGroup>(),
            _host.GetService<EnvironmentCreationOptionsTaskGroup>());

        // Send telemetry so we know which page in Dev Home the user clicked the create environment button.
        TelemetryFactory.Get<ITelemetry>().Log(
            "Create_Environment_button_Clicked",
            LogLevel.Critical,
            new EnvironmentRedirectionUserEvent(navigationAction: navigationAction, originPage),
            relatedActivityId: Orchestrator.ActivityId);
    }

    /// <summary>
    /// Starts a setup flow that only includes app management.
    /// </summary>
    [RelayCommand]
    private void StartAppManagement(string flowTitle)
    {
        _log.Information("Starting flow for app management");
        StartSetupFlowForTaskGroups(flowTitle, "AppManagement", _host.GetService<AppManagementTaskGroup>());
    }

    /// <summary>
    /// Opens the Windows settings app and redirects the user to the disks and volumes page.
    /// </summary>
    [RelayCommand]
    private async Task LaunchDisksAndVolumesSettingsPageAsync()
    {
        // Critical level approved by subhasan
        _log.Information("Launching settings on Disks and Volumes page");
        TelemetryFactory.Get<ITelemetry>().Log(
            "LaunchDisksAndVolumesSettingsPageTriggered",
            LogLevel.Critical,
            new DisksAndVolumesSettingsPageTriggeredEvent(source: "MainPageView"),
            Orchestrator.ActivityId);

        await Launcher.LaunchUriAsync(new Uri("ms-settings:disksandvolumes"));
    }

    /// <summary>
    /// Starts a setup flow that only includes configuration file.
    /// </summary>
    [RelayCommand]
    private async Task StartConfigurationFileAsync()
    {
        _log.Information("Launching configuration file flow");
        var configFileSetupFlow = _host.GetService<ConfigurationFileTaskGroup>();
        if (await configFileSetupFlow.PickConfigurationFileAsync())
        {
            _log.Information("Starting flow for Configuration file");
            StartSetupFlowForTaskGroups(null, "ConfigurationFile", configFileSetupFlow);
        }
    }

    [RelayCommand]
    private void HideAppInstallerUpdateNotification()
    {
        _log.Information("Hiding AppInstaller update notification");
        ShowAppInstallerUpdateNotification = false;
    }

    [RelayCommand]
    private async Task UpdateAppInstallerAsync()
    {
        HideAppInstallerUpdateNotification();
        _log.Information("Opening AppInstaller in the Store app");
        await Launcher.LaunchUriAsync(new Uri($"ms-windows-store://pdp/?productid={WindowsPackageManager.AppInstallerProductId}"));
    }

    [RelayCommand]
    private async Task OnLoadedAsync()
    {
        await Task.WhenAll(ValidateAppInstallerAsync(), ValidateConfigurationFileAsync());
    }

    private async Task<bool> ValidateConfigurationFileAsync()
    {
        return EnableConfigurationFileItem = await _dsc.IsUnstubbedAsync();
    }

    private async Task<bool> ValidateAppInstallerAsync()
    {
        return EnablePackageInstallerItem = await _wpm.IsAvailableAsync();
    }
}
