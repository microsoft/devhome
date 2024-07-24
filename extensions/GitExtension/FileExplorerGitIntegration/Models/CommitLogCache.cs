// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using LibGit2Sharp;

namespace FileExplorerGitIntegration.Models;

// LibGit2Sharp.CommitEnumerator are not reusable as they do not provide a way to reuse libgit2's revwalk object
// LibGit2's prepare_walk() does the expensive work of traversing and caching the commit graph
// Unfortunately LibGit2Sharp.CommitEnumerator.Reset() method resets the revwalk, but does not reinitialize the sort/push/hide options
// This leaves all that work wasted only to be done again.
// Furthermore, LibGit2 revwalk initialization takes locks on internal data, which causes contention in multithreaded scenarios as threads
// all scramble to initialize and re-initialize their own revwalk objects.
// Ideally, LibGit2Sharp improves the API to allow reusing the revwalk object, but that seems unlikely to happen soon.
internal sealed class CommitLogCache : IEnumerable<Commit>
{
    private readonly List<Commit> _commits = new();

    public CommitLogCache(Repository repo)
    {
        foreach (var commit in repo.Commits)
        {
            // For now, greedily get the entire commit log for simplicity.
            // PRO: No syncronization needed for the enumerator.
            // CON: May take longer for the initial load and use more memory.
            _commits.Add(commit);
        }
    }

    public IEnumerator<Commit> GetEnumerator()
    {
        return _commits.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
