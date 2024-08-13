// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.Database.DatabaseModels.RepositoryManagement;

public class RepositoryCommit
{
    public int RepositoryCommitId { get; set; }

    public Guid CommitHash { get; set; }

    public Uri? CommitUri { get; set; }

    public string? Author { get; set; }

    public DateTime CommitDateTime { get; set; }

    public int RepositoryId { get; set; }

    public Repository? Repository { get; set; }
}
