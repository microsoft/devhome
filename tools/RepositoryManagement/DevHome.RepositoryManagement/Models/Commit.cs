// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.RepositoryManagement.Models;

public class Commit
{
    public static readonly Commit DefaultCommit = new(string.Empty, TimeSpan.FromMinutes(0), string.Empty);

    public string Author { get; }

    public TimeSpan TimeSinceCommit { get; }

    public string SHA { get; }

    public Commit(string author, TimeSpan timeSinceCommit, string sHA)
    {
        Author = author;
        TimeSinceCommit = timeSinceCommit;
        SHA = sHA;
    }
}
