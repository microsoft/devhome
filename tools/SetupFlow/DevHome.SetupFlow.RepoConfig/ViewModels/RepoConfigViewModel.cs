// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.SetupFlow.RepoConfig.Models;
using Microsoft.UI.Xaml;

namespace DevHome.SetupFlow.RepoConfig.ViewModels;

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

    /// <summary>
    /// All repositories the user wants to clone.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CloningInformation> _repoReviewItems = new ();

    /// <summary>
    /// Controls if the "No repo" message is shown to the user.
    /// </summary>
    [ObservableProperty]
    private Visibility _shouldShowNoRepoMessage = Visibility.Visible;

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
        RepoDialogCancelled += _devDriveManager.CancelChangesToDevDrive;
        PageTitle = StringResource.GetLocalized(StringResourceKey.ReposConfigPageTitle);
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
        List<CloningInformation> repoReviewItems = new (RepoReviewItems);
        repoReviewItems.AddRange(cloningInformations);

        ShouldShowNoRepoMessage = Visibility.Collapsed;
        RepoReviewItems = new ObservableCollection<CloningInformation>(repoReviewItems);
        _taskGroup.SaveSetupTaskInformation(repoReviewItems);
    }

    /// <summary>
    /// Remove a specific cloning location from the list of repos to clone.
    /// </summary>
    /// <param name="cloningInformation">The cloning information to remove.</param>
    public void RemoveCloningInformation(CloningInformation cloningInformation)
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Removing repository {cloningInformation.RepositoryId} from repos to clone");
        RepoReviewItems.Remove(cloningInformation);
        UpdateCollection();

        if (RepoReviewItems.Count == 0)
        {
            ShouldShowNoRepoMessage = Visibility.Visible;
        }
    }

    // Assumes an item in the list has been changed via reference.
    public void UpdateCollection()
    {
        List<CloningInformation> repoReviewItems = new (RepoReviewItems);
        RepoReviewItems = new ObservableCollection<CloningInformation>(repoReviewItems);
        _taskGroup.SaveSetupTaskInformation(repoReviewItems);
    }

    /// <summary>
    /// Update the collection of items that are being cloned to the Dev Drive. With new information
    /// should the user choose to change the information with the customize button.
    /// </summary>
    /// <param name="cloningInfo">Cloning info that has a new path for the Dev Drive</param>
    public void UpdateCollectionWithDevDriveInfo(CloningInformation cloningInfo)
    {
        foreach (var item in RepoReviewItems)
        {
            if (item.CloneToDevDrive && item.CloningLocation != cloningInfo.CloningLocation)
            {
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
