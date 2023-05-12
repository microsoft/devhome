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
        NextPageButtonToolTipText = stringResource.GetLocalized(StringResourceKey.RepoToolNextButtonTooltip);
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
        // Need to worry about items being added via URL.
        // Because cloningInformation does not look at owning account repos that are added
        // with a different provider are not checked in euqality...is that bad though?
        // AddRepoViewModel checks if a repo is already added in the URL tab.
        // Changed the equality to owningAccountName now two forks are different repos.
        // It is up to the user to clone the forks in different locations though.
        // Some things we need to worry about
        // 1. THe user can be adding repos from a different provider and account
        // 2. The user can be adding repos from the same provider but different accounts
        // 3. The user can be removing repos from a specific provider and account.
        // Now that the repos can be de-selected we need to find those deselected repos
        // and remove them from RepoReviewItems.
        // But.  If the user unselects a repo it won't be in cloninInformations.
        // A CloningInformation has the provider name, account name, and repo name.
        // If a user leavs the repo selected we don't want to re-add it.  So, check against that.
        // If a user had something selected then de selects it it won't be in cloningInformations but will
        // be in RepoReview items.  Is that good enough to say "You don't want to clone this anymore?

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

        // Handle the case that a user de-selected a repo from re-opening the repo tool.
        // This is the case where RepoReviewItems does not contain a repo in cloningInformations.
        // What about the case where repo sare added and nothing is deselected either because a different provider
        // or account is being used?
        // cloningInformation.equals uses the provider name and repo name for equality.
        // owningaccount is not used for equality because the owner is null when adding via URL.
        // everythingtoclone has all pre-selected repos when the repo tool is opened again.
        // so yeah, if RepoReviewItems has the repo and cloningInformation does not
        // they de-selected it.
        // RemoveCloningInformation calls save.  If we don't call RemoveCloningInformation repo tool
        // should call save.
        var shouldCallSave = true;

        var deSelectedRepos = RepoReviewItems.Except(cloningInformations).ToList();
        foreach (var deSelectedRepo in deSelectedRepos)
        {
            RemoveCloningInformation(deSelectedRepo);
            shouldCallSave = false;
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
    /// Event that the Dev Drive manager can subscribe to, to know when if the Add repo or edit clone path
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
