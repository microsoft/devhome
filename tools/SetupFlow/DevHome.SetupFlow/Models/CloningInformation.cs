// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Used to shuttle information between RepoConfigView and AddRepoDialog.
/// Specifically this contains all information needed to clone repositories to a user's machine.
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

    public string PluginName
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the repository is to be cloned on a Dev Drive.
    /// </summary>
    public bool CloneToDevDrive
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether used specifically for telemetry to get insights if repos are being cloned to an existing devdrive.
    /// </summary>
    public bool CloneToExistingDevDrive
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating the alias associated with the Dev drive in the form of
    /// "Drive label" (Drive letter:) [Size GB/TB]. E.g Dev Drive (D:) [50.0 GB]
    /// </summary>
    [ObservableProperty]
    private string _cloneLocationAlias;

    /// <summary>
    /// Gets the repo name and formats it for the Repo Review view.
    /// </summary>
    public string RepositoryId => $"{RepositoryToClone.DisplayName ?? string.Empty}";

    /// <summary>
    /// Gets the repository in a [organization]\[reponame] style
    /// </summary>
    public string RepositoryOwnerAndName => Path.Join(RepositoryToClone.OwningAccountName ?? string.Empty, RepositoryToClone.DisplayName);

    /// <summary>
    /// Gets the clone path the user wants to clone the repo to.
    /// </summary>
    public string ClonePath
    {
        get
        {
            var path = CloningLocation.FullName;

            if (RepositoryToClone != null)
            {
                path = Path.Join(path, RepositoryToClone.DisplayName);
            }

            return path;
        }
    }

    /// <summary>
    /// Gets or sets the name of the button that allows a user to edit the clone path of a repository.
    /// This name can't be static because each button name needs to be unique.  Because each name needs to be unique
    /// the name is stored here so it can be set at the time when a unique name can be made.
    /// </summary>
    public string EditClonePathAutomationName
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the name of the button that allows a user to remove the repository from being cloned.
    /// This name can't be static because each button name needs to be unique.  Because each name needs to be unique
    /// the name is stored here so it can be set at the time when a unique name can be made.
    /// </summary>
    public string RemoveFromCloningAutomationName
    {
        get; set;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CloningInformation"/> class.
    /// Public constructor for XAML view to construct a CLoningInformation
    /// </summary>
    public CloningInformation()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CloningInformation"/> class.
    /// </summary>
    /// <param name="repoToClone">The repo to clone</param>
    public CloningInformation(IRepository repoToClone)
    {
        RepositoryToClone = repoToClone;
    }

    /// <summary>
    /// Compares two CloningInformations for equality.
    /// </summary>
    /// <param name="other">The CloningInformation to compare to.</param>
    /// <returns>True if equal.</returns>
    /// <remarks>
    /// ProviderName, and RepositoryToClone are used for equality.
    /// </remarks>
    public bool Equals(CloningInformation other)
    {
        if (other == null)
        {
            return false;
        }

        return ProviderName.Equals(other.ProviderName, StringComparison.OrdinalIgnoreCase) &&
            RepositoryToClone.OwningAccountName.Equals(other.RepositoryToClone.OwningAccountName, StringComparison.OrdinalIgnoreCase) &&
            RepositoryToClone.DisplayName.Equals(other.RepositoryToClone.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as CloningInformation);
    }

    public override int GetHashCode()
    {
        return (ProviderName + RepositoryToClone.DisplayName).GetHashCode();
    }
}
