// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Database.DatabaseModels.RepositoryManagement;

public class Repository
{
    public int RepositoryId { get; set; }

    public string? RepositoryName { get; set; }

    public string? RepositoryClonePath { get; set; }

    public string? LocalBranchName { get; set; }

    public List<RepositoryCommit>? RemoteCommits { get; set; }

    public RepositoryManagement? RepositoryManagement { get; set; }
}
