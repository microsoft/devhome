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

/// <summary>.
/// Contains all information needed to clone repositories to a user's machine.
/// One CloneInformation per repository
/// </summary>
public partial class CloningInformation : ObservableObject, IEquatable<CloningInformation>
{
    /// <summary>
    /// Gets or sets the repository the user wants to clone.
    /// </summary>
    public IRepository RepositoryToClone
    {
        get; set;
    }

    /// <summary>
    /// Full path the user wants to clone the repository.
    /// RepoConfigTaskGroup appends other directories at the end of CloningLocation to make sure that
    /// two repositories aren't cloned to the same location.
    /// </summary>
    [ObservableProperty]
    private DirectoryInfo _cloningLocation;

    /// <summary>
    /// Gets or sets the account that owns the repository.
    /// </summary>
    public IDeveloperId OwningAccount
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the name of the repository provider that the user used to log into their account.
    /// </summary>
    public string ProviderName
    {
        get; set;
    }

    /// <summary>
    /// Gets the repo name and formats it for the Repo Review view.
    /// </summary>
    public string RepositoryId => $"{OwningAccount.LoginId() ?? string.Empty}/{RepositoryToClone.DisplayName() ?? string.Empty}";

    /// <summary>
    /// Gets the clone path the user wants ot clone the repo to.
    /// </summary>
    public string ClonePath => CloningLocation.FullName ?? string.Empty;

    /// <summary>
    /// Compares two CloningInformations for equality.
    /// </summary>
    /// <param name="other">The CloningInformation to compare to.</param>
    /// <returns>True if equal.</returns>
    /// <remarks>
    /// ProviderName, OwningAccount, and RepositoryToClone are used for equality.
    /// </remarks>
    public bool Equals(CloningInformation other)
    {
        if (other == null)
        {
            return false;
        }
        else
        {
            RepositoriesToClone[account].Remove(repositoryToAddOrRemove);
            if (RepositoriesToClone[account].Count == 0)
            {
                RepositoriesToClone.Remove(account);
            }
        }

        return ProviderName.Equals(other.ProviderName, StringComparison.OrdinalIgnoreCase) &&
            OwningAccount.LoginId().Equals(other.OwningAccount.LoginId(), StringComparison.OrdinalIgnoreCase) &&
            RepositoryToClone.DisplayName().Equals(other.RepositoryToClone.DisplayName(), StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as CloningInformation);
    }

    public override int GetHashCode()
    {
        return (ProviderName + OwningAccount.LoginId() + RepositoryToClone.DisplayName()).GetHashCode();
    }
}
