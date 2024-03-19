﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

extern alias Projection;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;
using WinUIEx;

namespace DevHome.SetupFlow.ViewModels;

public partial class ReviewViewModel : SetupPageViewModelBase
{
    private readonly SetupFlowOrchestrator _setupFlowOrchestrator;
    private readonly ConfigurationFileBuilder _configFileBuilder;
    private readonly WindowEx _mainWindow;

    [ObservableProperty]
    private IList<ReviewTabViewModelBase> _reviewTabs;

    [ObservableProperty]
    private ReviewTabViewModelBase _selectedReviewTab;

    [ObservableProperty]
    private bool _readAndAgree;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SetUpCommand))]
    private bool _canSetUp;

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

    public ReviewViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        ConfigurationFileBuilder configFileBuilder,
        WindowEx mainWindow)
        : base(stringResource, orchestrator)
    {
        NextPageButtonText = StringResource.GetLocalized(StringResourceKey.SetUpButton);
        PageTitle = StringResource.GetLocalized(StringResourceKey.ReviewPageTitle);

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

            await Orchestrator.GoToNextPage();
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError(Log.Component.Review, $"Failed to initialize elevated process.", e);
        }
    }

    [RelayCommand]
    private void DownloadConfiguration()
    {
        var result = _mainWindow.SaveFileDialog(null, ("*.winget", "YAML file"));
        if (result != null)
        {
            var fileName = result.Value.Item1;
            var configFile = _configFileBuilder.BuildConfigFileStringFromTaskGroups(Orchestrator.TaskGroups, ConfigurationFileKind.Normal);
            File.WriteAllText(fileName, configFile);
        }
    }
}
