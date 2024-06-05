// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

extern alias Projection;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.Common.Windows.FileDialog;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.SetupFlow.ViewModels;

public partial class ReviewViewModel : SetupPageViewModelBase
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ReviewViewModel));

    private readonly SetupFlowOrchestrator _setupFlowOrchestrator;
    private readonly ConfigurationFileBuilder _configFileBuilder;
    private readonly Window _mainWindow;

    [ObservableProperty]
    private IList<ReviewTabViewModelBase> _reviewTabs;

    [ObservableProperty]
    private ReviewTabViewModelBase _selectedReviewTab;

    [ObservableProperty]
    private bool _readAndAgree;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SetUpCommand))]
    private bool _canSetUp;

    [ObservableProperty]
    private string _reviewPageTitle;

    [ObservableProperty]
    private string _reviewPageExpanderDescription;

    [ObservableProperty]
    private string _reviewPageDescription;

    public bool ShouldShowGenerateConfigurationFile => !Orchestrator.IsInCreateEnvironmentFlow;

    public bool HasApplicationsToInstall => Orchestrator.GetTaskGroup<AppManagementTaskGroup>()?.SetupTasks.Any() == true;

    public bool RequiresTermsAgreement => HasApplicationsToInstall;

    public bool HasMSStoreApplicationsToInstall
    {
        get
        {
            var hasMSStoreApps = Orchestrator.GetTaskGroup<AppManagementTaskGroup>()?.SetupTasks.Any(task =>
            {
                var installTask = task as InstallPackageTask;
                return installTask?.IsFromMSStore == true;
            });

            return hasMSStoreApps == true;
        }
    }

    public bool CanSetupTarget
    {
        get
        {
            var repoConfigTasksTotal = _setupFlowOrchestrator.GetTaskGroup<RepoConfigTaskGroup>()?.CloneTasks.Count ?? 0;
            var appManagementTasksTotal = _setupFlowOrchestrator.GetTaskGroup<AppManagementTaskGroup>()?.SetupTasks.Count() ?? 0;
            if (_setupFlowOrchestrator.IsSettingUpATargetMachine && repoConfigTasksTotal == 0 && appManagementTasksTotal == 0)
            {
                // either repo config or app management task group is required to setup target
                return false;
            }

            return true;
        }
    }

    public bool HasTasksToSetUp => Orchestrator.TaskGroups.Any(g => g.SetupTasks.Any());

    public bool HasDSCTasksToDownload => Orchestrator.TaskGroups.Any(g => g.DSCTasks.Any());

    public ReviewViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        ConfigurationFileBuilder configFileBuilder,
        Window mainWindow)
        : base(stringResource, orchestrator)
    {
        NextPageButtonText = StringResource.GetLocalized(StringResourceKey.SetUpButton);
        PageTitle = StringResource.GetLocalized(StringResourceKey.ReviewPageTitle);
        ReviewPageExpanderDescription = StringResource.GetLocalized(StringResourceKey.ReviewExpanderDescription);
        ReviewPageDescription = StringResource.GetLocalized(StringResourceKey.SetupShellReviewPageDescription);

        _setupFlowOrchestrator = orchestrator;
        _configFileBuilder = configFileBuilder;
        _mainWindow = mainWindow;
    }

    protected async override Task OnEachNavigateToAsync()
    {
        // Re-compute the list of tabs as it can change depending on the current selections
        ReviewTabs =
            Orchestrator.TaskGroups
            .Select(taskGroup => taskGroup.GetReviewTabViewModel())
            .Where(tab => tab?.HasItems == true)
            .ToList();
        SelectedReviewTab = ReviewTabs.FirstOrDefault();

        // If the CreateEnvironmentTaskGroup is present, update the setup button text to "Create Environment"
        // and page title to "Review your environment"
        if (Orchestrator.GetTaskGroup<EnvironmentCreationOptionsTaskGroup>() != null)
        {
            NextPageButtonText = StringResource.GetLocalized(StringResourceKey.CreateEnvironmentButtonText);
            PageTitle = StringResource.GetLocalized(StringResourceKey.EnvironmentCreationReviewPageTitle);
            ReviewPageExpanderDescription = StringResource.GetLocalized(StringResourceKey.EnvironmentCreationReviewExpanderDescription);
            ReviewPageDescription = StringResource.GetLocalized(StringResourceKey.SetupShellReviewPageDescriptionForEnvironmentCreation);
        }

        NextPageButtonToolTipText = HasTasksToSetUp ? null : StringResource.GetLocalized(StringResourceKey.ReviewNothingToSetUpToolTip);

        UpdateCanSetUp();

        await Task.CompletedTask;
    }

    partial void OnReadAndAgreeChanged(bool value) => UpdateCanSetUp();

    public void UpdateCanSetUp()
    {
        CanSetUp = HasTasksToSetUp && IsValidTermsAgreement() && CanSetupTarget;
    }

    /// <summary>
    /// Validate if the terms agreement is required and checked
    /// </summary>
    /// <returns>True if terms agreement is valid, false otherwise.</returns>
    private bool IsValidTermsAgreement()
    {
        return !RequiresTermsAgreement || ReadAndAgree;
    }

    [RelayCommand(CanExecute = nameof(CanSetUp))]
    private async Task OnSetUpAsync()
    {
        try
        {
            // If we are in the setup target flow, we don't need to initialize the elevated server.
            // as work will be done in a remote machine.
            if (!Orchestrator.IsSettingUpATargetMachine)
            {
                await Orchestrator.InitializeElevatedServerAsync();
            }

            var flowPages = Orchestrator.FlowPages.Select(p => p.GetType().Name).ToList();
            TelemetryFactory.Get<ITelemetry>().Log("Review_SetUp", LogLevel.Critical, new ReviewSetUpCommandEvent(Orchestrator.IsSettingUpATargetMachine, flowPages));
            await Orchestrator.GoToNextPage();
        }
        catch (Exception e)
        {
            _log.Error(e, $"Failed to initialize elevated process.");
        }
    }

    [RelayCommand(CanExecute = nameof(HasDSCTasksToDownload))]
    private async Task DownloadConfigurationAsync()
    {
        try
        {
            // Show the save file dialog
            using var fileDialog = new WindowSaveFileDialog();
            fileDialog.AddFileType(StringResource.GetLocalized(StringResourceKey.FilePickerSingleFileTypeOption, "YAML"), ".winget");
            fileDialog.AddFileType(StringResource.GetLocalized(StringResourceKey.FilePickerSingleFileTypeOption, "YAML"), ".dsc.yaml");
            var fileName = fileDialog.Show(_mainWindow);

            // If the user selected a file, write the configuration to it
            if (!string.IsNullOrEmpty(fileName))
            {
                var configFile = _configFileBuilder.BuildConfigFileStringFromTaskGroups(Orchestrator.TaskGroups, ConfigurationFileKind.Normal);
                await File.WriteAllTextAsync(fileName, configFile);
                ReportGenerateConfiguration();
            }
        }
        catch (Exception e)
        {
            _log.Error(e, $"Failed to download configuration file.");
        }
    }

    private void ReportGenerateConfiguration()
    {
        var flowPages = Orchestrator.FlowPages.Select(p => p.GetType().Name).ToList();
        TelemetryFactory.Get<ITelemetry>().Log("Review_GenerateConfiguration", LogLevel.Critical, new ReviewGenerateConfigurationCommandEvent(flowPages));

        var installTasks = Orchestrator.TaskGroups.OfType<AppManagementTaskGroup>()
            .SelectMany(x => x.DSCTasks.OfType<InstallPackageTask>());

        var installedPackagesCount = installTasks.Count(task => task.IsInstalled);
        var nonInstalledPackagesCount = installTasks.Count() - installedPackagesCount;
        TelemetryFactory.Get<ITelemetry>().Log("Review_GenerateConfigurationForInstallPackages", LogLevel.Critical, new ReviewGenerateConfigurationForInstallEvent(installedPackagesCount, nonInstalledPackagesCount));
    }
}
