// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models;
public partial class RepoViewListItem : ObservableObject
{
    public bool IsPrivate { get; }

    public string RepoName { get; }

    public RepoViewListItem(IRepository repo)
    {
        IsPrivate = repo.IsPrivate;
        RepoName = repo.DisplayName;
    }
}
