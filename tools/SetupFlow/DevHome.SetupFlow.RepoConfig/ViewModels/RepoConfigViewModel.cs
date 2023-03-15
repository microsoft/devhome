// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.SetupFlow.RepoConfig.Models;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;

namespace DevHome.SetupFlow.RepoConfig.ViewModels;

/// <summary>
/// The view model to handle the whole repo tool.
/// </summary>
public partial class RepoConfigViewModel : SetupPageViewModelBase
{
    /// <summary>
    /// The logger to use to log things.
    /// </summary>
    private readonly ILogger _logger;

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

    public RepoConfigViewModel(ILogger logger, IStringResource stringResource, IDevDriveManager devDriveManager, RepoConfigTaskGroup taskGroup)
        : base(stringResource)
    {
        _logger = logger;
        _taskGroup = taskGroup;
        _devDriveManager = devDriveManager;
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
}
