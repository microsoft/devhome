// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.SetupFlow.RepoConfig.Models;
using DevHome.Telemetry;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.RepoConfig.ViewModels;

public partial class RepoConfigViewModel : SetupPageViewModelBase
{
    private readonly ILogger _logger;
    private readonly RepoConfigTaskGroup _taskGroup;

    public RepoConfigViewModel(ILogger logger, IStringResource stringResource, RepoConfigTaskGroup taskGroup)
        : base(stringResource)
    {
        _logger = logger;
        _taskGroup = taskGroup;
    }

    /// <summary>
    /// Converts the location and repositories to a list of CloneRepo tasks.
    /// </summary>
    /// <param name="cloningInformation">All repositories the user selected.  Can be 1 if the user types in a git URL</param>
    public void SaveSetupTaskInformation(CloningInformation cloningInformation)
    {
        _taskGroup.SaveSetupTaskInformation(cloningInformation);
    }

    public void SaveSetupTaskInformation(DirectoryInfo path, IRepository repoToClone)
    {
        _taskGroup.SaveSetupTaskInformation(path, repoToClone);
    }

    public void SaveRepoInformation(CloningInformation cloningInformation)
    {
        // When adding via URL the user can enter either
        // 1. A URL
        // 2. A Username/RepositoryName
        // At the moment, this assumes that the user will enter a url that ends with .git.
        // Need to make this provider agnostic.
        if (cloningInformation.CurrentPage == Models.Common.CurrentPage.AddViaUrl)
        {
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
                repoName = repoName.Substring(0, repoName.IndexOf('.'));
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
                    if (nameParts.Length > 2)
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
        }
    }
}
