// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Database.DatabaseModels.RepositoryManagement;

public class RepositoryCommit
{
    public int RepositoryCommitId { get; set; }

    public Guid CommitHash { get; set; }

    public Uri? CommitUri { get; set; }

    public string? Author { get; set; }

    public DateTime CommitDateTime { get; set; }

    // N:1 relationship.  RepositoryCommit is the dependant and needs the Id and object.
    public int RepositoryId { get; set; }

    public Repository? Repository { get; set; }
}
