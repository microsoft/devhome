// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Windows.DevHome.SDK;
using static DevHome.SetupFlow.RepoConfig.Models.Common;

namespace DevHome.SetupFlow.RepoConfig.Models;

/// <summary>
/// Used to shuttle information between RepoConfigView and AddRepoDialog.
/// Specifically this contains all information needed to clone repositories to a user's machine.
/// </summary>
public class CloningInformation
{
    internal string UrlOrUsernameRepo
    {
        get; set;
    }

    internal Dictionary<IDeveloperId, List<IRepository>> RepositoriesToClone
    {
        get; set;
    }

    internal DirectoryInfo CloneLocation
    {
        get; set;
    }

    internal CurrentPage CurrentPage
    {
        get; set;
    }

    internal CloneLocationSelectionMethod CloneLocationSelectionMethod
    {
        get; set;
    }

    public CloningInformation()
    {
        UrlOrUsernameRepo = string.Empty;
        RepositoriesToClone = new ();
        CloneLocation = null;
        CurrentPage = CurrentPage.AddViaUrl;
        CloneLocationSelectionMethod = CloneLocationSelectionMethod.LocalPath;
    }

    /// <summary>
    /// If the repository exists, it is removed from the list.
    /// If the repository does not exist, it is added to the list.
    /// </summary>
    /// <param name="account">Used to find the repository</param>
    /// <param name="repositoryToAddOrRemove">The repository to add or remove from the list</param>
    public void AddRepositoryOrRemoveIfExists(IDeveloperId account, IRepository repositoryToAddOrRemove)
    {
        RepositoriesToClone.TryAdd(account, new List<IRepository>());

        if (!RepositoriesToClone[account].Any(x => x.DisplayName.Equals(repositoryToAddOrRemove.DisplayName, StringComparison.OrdinalIgnoreCase)))
        {
            RepositoriesToClone[account].Add(repositoryToAddOrRemove);
        }
        else
        {
            RepositoriesToClone[account].Remove(repositoryToAddOrRemove);
        }
    }
}
