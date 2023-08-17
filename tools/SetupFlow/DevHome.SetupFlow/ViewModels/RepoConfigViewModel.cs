// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;

namespace DevHome.SetupFlow.ViewModels;

/// <summary>
/// The view model to handle the whole repo tool.
/// </summary>
public partial class RepoConfigViewModel : SetupPageViewModelBase
{
    /// <summary>
    /// All the tasks that need to be ran during the loading page.
    /// </summary>
    private readonly RepoConfigTaskGroup _taskGroup;

    private readonly IDevDriveManager _devDriveManager;

    public ISetupFlowStringResource LocalStringResource { get; }

    /// <summary>
    /// All repositories the user wants to clone.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CloningInformation> _repoReviewItems = new ();

    public IDevDriveManager DevDriveManager => _devDriveManager;

    public RepoConfigViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        IDevDriveManager devDriveManager,
        RepoConfigTaskGroup taskGroup)
        : base(stringResource, orchestrator)
    {
        _taskGroup = taskGroup;
        _devDriveManager = devDriveManager;
        LocalStringResource = stringResource;
        RepoDialogCancelled += _devDriveManager.CancelChangesToDevDrive;
        PageTitle = StringResource.GetLocalized(StringResourceKey.ReposConfigPageTitle);
        NextPageButtonTeachingTipText = stringResource.GetLocalized(StringResourceKey.RepoToolNextButtonTooltip);
    }

    /// <summary>
    /// Saves all cloning informations to be cloned during the loading screen.
    /// </summary>
    /// <param name="cloningInformations">All repositories the user selected.</param>
    /// <remarks>
    /// Makes a new collection to force UI to update.
    /// </remarks>
    public void SaveSetupTaskInformation(List<CloningInformation> cloningInformations)
    {
        // Handle the case where a user re-opens the repo tool with repos that are already selected
        // Remove them from cloninginformations so they aren't added again.
        var alreadyAddedRepos = RepoReviewItems.Intersect(cloningInformations).ToList();

        var localCloningInfos = new List<CloningInformation>(cloningInformations);
        foreach (var alreadyAddedRepo in alreadyAddedRepos)
        {
            localCloningInfos.Remove(alreadyAddedRepo);
        }

        foreach (var cloningInformation in localCloningInfos)
        {
            RepoReviewItems.Add(cloningInformation);
        }

        // RemoveCloningInformation calls save.  If we don't call RemoveCloningInformation repo tool
        // should call save.
        var shouldCallSave = true;

        // Handle the case that a user de-selected a repo from re-opening the repo tool.
        // This is the case where RepoReviewItems does not contain a repo in cloningInformations.
        var deSelectedRepos = RepoReviewItems.Except(cloningInformations).ToList();
        foreach (var deSelectedRepo in deSelectedRepos)
        {
            // Ignore repos added via URL.  They would get removed here.
            if (deSelectedRepo.OwningAccount != null)
            {
                RemoveCloningInformation(deSelectedRepo);
                shouldCallSave = false;
            }
        }

        if (shouldCallSave)
        {
            RepoReviewItems = new ObservableCollection<CloningInformation>(RepoReviewItems);
            _taskGroup.SaveSetupTaskInformation(RepoReviewItems.ToList());
        }
    }

    /// <summary>
    /// Remove a specific cloning location from the list of repos to clone.
    /// </summary>
    /// <param name="cloningInformation">The cloning information to remove.</param>
    public void RemoveCloningInformation(CloningInformation cloningInformation)
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Removing repository {cloningInformation.RepositoryId} from repos to clone");
        RepoReviewItems.Remove(cloningInformation);

        // force collection to be empty(?) converter won't fire otherwise.
        if (RepoReviewItems.Count == 0)
        {
            RepoReviewItems = new ObservableCollection<CloningInformation>();
        }

        _taskGroup.SaveSetupTaskInformation(RepoReviewItems.ToList());
    }

    public void UpdateCloneLocation(CloningInformation cloningInformation)
    {
        var location = RepoReviewItems.IndexOf(cloningInformation);
        if (location != -1)
        {
            RepoReviewItems[location] = cloningInformation;
            _taskGroup.SaveSetupTaskInformation(RepoReviewItems.ToList());
        }
    }

    /// <summary>
    /// Update the collection of items that are being cloned to the Dev Drive. With new information
    /// should the user choose to change the information with the customize button.
    /// </summary>
    /// <param name="cloningInfo">Cloning info that has a new path for the Dev Drive</param>
    public void UpdateCollectionWithDevDriveInfo(CloningInformation cloningInfo)
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Updating dev drive location on repos to clone after change to dev drive");
        foreach (var item in RepoReviewItems)
        {
            if (item.CloneToDevDrive && item.CloningLocation != cloningInfo.CloningLocation)
            {
                Log.Logger?.ReportDebug(Log.Component.RepoConfig, $"Updating {item.RepositoryId}");
                item.CloningLocation = new System.IO.DirectoryInfo(cloningInfo.CloningLocation.FullName);
                item.CloneLocationAlias = cloningInfo.CloneLocationAlias;
            }
        }
    }

    /// <summary>
    /// Event that the Dev Drive manager can subscribe to, to know when and if the Add repo or edit clone path
    /// dialogs closed using the cancel button.
    /// </summary>
    /// <remarks>
    /// This will send back the original Dev Drive object back to the Dev Drive manager who will update its
    /// list. This is because clicking the save button in the Dev Drive window will overwrite the Dev Drive
    /// information. However, the user can still select cancel from one of the repo dialogs. Selecting cancel
    /// there should revert the changes made to the Dev Drive object the manager hold.
    /// </remarks>
    public event Action RepoDialogCancelled = () => { };

    public void ReportDialogCancellation()
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Repo add/edit dialog cancelled");
        RepoDialogCancelled();
    }
}
