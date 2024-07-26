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
        // For now, greedily get the entire commit log for simplicity.
        // PRO: No syncronization needed for the enumerator.
        // CON: May take longer for the initial load and use more memory.
        // For reference, I tested on my dev machine on a repo with an *enormous* number of commits
        // https://github.com/python/cpython with > 120k commits. This was a one-time cost of 2-3 seconds, but also
        // consumed several hundred MB of memory.

        // Often, but not always, the root folder has some boilerplate/doc/config that rarely changes
        // Therefore, populating the last commit for each file in the root folder often requires a large portion of the commit history anyway.
        // This somewhat blunts the appeal of trying to load this incrementally.
        _commits.AddRange(repo.Commits);
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
