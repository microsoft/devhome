// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.RepositoryManagement.Models;

public class Commit
{
    public static readonly Commit DefaultCommit = new(string.Empty, DateTime.MinValue, string.Empty);

    public string Author { get; }

    public DateTime CommitDateTime { get; }

    public string SHA { get; }

    public Commit(string author, DateTime commitDateTime, string sHA)
    {
        Author = author;
        CommitDateTime = commitDateTime;
        SHA = sHA;
    }
}
