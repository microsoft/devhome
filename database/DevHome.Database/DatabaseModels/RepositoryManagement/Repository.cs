// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Database.DatabaseModels.RepositoryManagement;

public class Repository
{
    public int RepositoryId { get; set; }

    public string? RepositoryName { get; set; }

    public string? RepositoryClonePath { get; set; }

    public string? LocalBranchName { get; set; }

    // 1:N relationship.  Repository needs only the object.
    public List<RepositoryCommit>? RemoteCommits { get; set; }

    // 1:1 relationship.  Repository is the parent and needs only the object.
    public RepositoryMetadata? RepositoryMetadata { get; set; }
}
