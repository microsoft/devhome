// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Class used to house the repo name and IsPrivate.  IsPrivate is used in views to determine
/// the glyph to show.  These are in a model for DataType in an ItemTemplate.
/// </summary>
public partial class RepoViewListItem : ObservableObject
{
    /// <summary>
    /// Gets a value indicating whether the repo is a private repo.  If changed to "IsPublic" the
    /// values of the converters in the views need to change order.
    /// </summary>
    public bool IsPrivate { get; }

    /// <summary>
    /// Gets the name of the repository
    /// </summary>
    public string RepoName { get; }

    /// <summary>
    /// Gets the name of the organization that owns this repo.
    /// </summary>
    public string OwningAccountName { get; }

    public string RepoDisplayName => Path.Join(OwningAccountName, RepoName);

    public RepoViewListItem(IRepository repo)
    {
        IsPrivate = repo.IsPrivate;
        RepoName = repo.DisplayName;
        OwningAccountName = repo.OwningAccountName;
    }
}
