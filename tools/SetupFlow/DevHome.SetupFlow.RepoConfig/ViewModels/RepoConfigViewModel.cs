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

    public RepoConfigViewModel(ILogger logger, IStringResource stringResource, RepoConfigTaskGroup taskGroup)
        : base(stringResource)
    {
        _logger = logger;
        _taskGroup = taskGroup;
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
<<<<<<< HEAD
            // Get all information to figure out what the user entered.
            var repoName = string.Empty;
            var urlOrUsernameAndRepo = cloningInformation.UrlOrUsernameRepo;
            var cloneUrlOrRepoName = string.Empty;

            // if Test ends with .git assume url.
            if (urlOrUsernameAndRepo.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            {
                cloneUrlOrRepoName = urlOrUsernameAndRepo;

                // Get the repo name from the url
                var urlParts = urlOrUsernameAndRepo.Split('/');

                // Get reponame.git
                repoName = urlParts[urlParts.Length - 1];

                // substring out .git
                repoName = repoName.Substring(0, repoName.LastIndexOf('.'));
            }
            else
            {
                if (Uri.TryCreate(urlOrUsernameAndRepo, UriKind.Absolute, out var url))
                {
                    cloneUrlOrRepoName = urlOrUsernameAndRepo;
                    repoName = url.Segments[url.Segments.Length - 1].TrimEnd('/');
                }
                else
                {
                    // username/Repo
                    var nameParts = urlOrUsernameAndRepo.Split("/");
                    if (nameParts.Length != 2)
                    {
                        _logger.Log("Invalid repo name. Expected format: username/RepoName", LogLevel.Local, urlOrUsernameAndRepo);
                        return;
                    }

                    repoName = nameParts[1];
                    cloneUrlOrRepoName = "https://github.com/" + urlOrUsernameAndRepo;
                }

                if (!cloneUrlOrRepoName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                {
                    cloneUrlOrRepoName += ".git";
                }
            }

            var repoToClone = new Repository(repoName, cloneUrlOrRepoName);
            SaveSetupTaskInformation(cloningInformation.CloneLocation, repoToClone);
        }
        else
        {
            SaveSetupTaskInformation(cloningInformation);
=======
            ShouldShowNoRepoMessage = Visibility.Visible;
>>>>>>> DartRepo/user/dahoehna/UIDevDriveAndRefactoring
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
