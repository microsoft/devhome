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
internal sealed class CommitLogCache
{
    private readonly List<Commit> _commits = new();
    private readonly string _workingDirectory;

    // For now, we'll use the command line to get the last commit for a file, on demand.
    // In the future we may use some sort of heuristic to determine if we should use the command line or not.
    private readonly bool _preferCommandLine = true;
    private readonly bool _useCommandLine;
    private readonly GitDetect _gitDetect = new();
    private readonly bool _gitInstalled;

    private readonly LruCacheDictionary<string, CommitWrapper> _cache = new();

    public CommitLogCache(Repository repo)
    {
        _workingDirectory = repo.Info.WorkingDirectory;

        // Use the command line to get the last commit for a file, on demand.
        // PRO: If Git is installed, this will always succeed, and in a somewhat predictable amount of time.
        //      Doesn't consume memory for the entire commit log.
        // CON: Spawning a process for each file may be slower than walking to recent commits.
        //      Does not work if Git isn't installed.
        if (_preferCommandLine)
        {
            _gitInstalled = _gitDetect.DetectGit();
            _useCommandLine = _gitInstalled;
        }

        if (!_useCommandLine)
        {
            // Greedily get the entire commit log for simplicity.
            // PRO: No syncronization needed for the enumerator.
            // CON: May take longer for the initial load and use more memory.
            // For reference, I tested on my dev machine on a repo with an *large* number of commits
            // https://github.com/python/cpython with > 120k commits. This was a one-time cost of 2-3 seconds, but also
            // consumed several hundred MB of memory.
            // Unfortunately, loading an *enormous* repo with 1M+ commits consumes a multiple GBs of memory.

            // For smaller repos this method is faster, but the memory consumption is prohibitive on the huge ones.
            // Additionally, virtualized repos (aka GVFS) may show the entire commit log, but each commit's tree isn't always hydrated.
            // As a result, GVFS repos often fail to find the last commit for a file if it is older than some unknown threshold.

            // Often, but not always, the root folder has some boilerplate/doc/config that rarely changes
            // Therefore, populating the last commit for each file in the root folder often requires a large portion of the commit history anyway.
            // This somewhat blunts the appeal of trying to load this incrementally.
            _commits.AddRange(repo.Commits);
        }
    }

    public CommitWrapper? FindLastCommit(string relativePath)
    {
        if (_cache.TryGetValue(relativePath, out var cachedCommit))
        {
            return cachedCommit;
        }

        CommitWrapper? result;
        if (_useCommandLine)
        {
            result = FindLastCommitUsingCommandLine(relativePath);
        }
        else
        {
            result = FindLastCommitUsingLibGit2Sharp(relativePath);
        }

        if (result != null)
        {
            result = _cache.GetOrAdd(relativePath, result);
        }

        return result;
    }

    private CommitWrapper? FindLastCommitUsingCommandLine(string relativePath)
    {
        if (relativePath == string.Empty)
        {
            relativePath = ".";
        }

        var fullPath = Path.Combine(_workingDirectory, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        var filename = Path.GetFileName(fullPath);
        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(filename))
        {
            return null;
        }

        var result = GitExecute.ExecuteGitCommand(_gitDetect.GitConfiguration.ReadInstallPath(), directory, $"log -n 1 --pretty=format:%s%n%an%n%ae%n%aI%n%H -- {filename}");
        if ((result.Status != Microsoft.Windows.DevHome.SDK.ProviderOperationStatus.Success) || (result.Output is null))
        {
            return null;
        }

        var parts = result.Output.Split('\n');
        if (parts.Length != 5)
        {
            return null;
        }

        string message = parts[0];
        string authorName = parts[1];
        string authorEmail = parts[2];
        DateTimeOffset authorWhen = DateTimeOffset.Parse(parts[3], null, System.Globalization.DateTimeStyles.RoundtripKind);
        string sha = parts[4];
        return new CommitWrapper(message, authorName, authorEmail, authorWhen, sha);
    }

    private CommitWrapper? FindLastCommitUsingLibGit2Sharp(string relativePath)
    {
        var checkedFirstCommit = false;
        foreach (var currentCommit in _commits)
        {
            // Now that CommitLogCache is caching the result of the revwalk, the next piece that is most expensive
            // is obtaining relativePath's TreeEntry from the Tree (e.g. currentTree[relativePath].
            // Digging into the git shows that number of directory entries and/or directory depth may play a factor.
            // There may also be a lot of redundant lookups happening here, so it may make sense to do some LRU caching.
            var currentTree = currentCommit.Tree;
            var currentTreeEntry = currentTree[relativePath];
            if (currentTreeEntry == null)
            {
                if (checkedFirstCommit)
                {
                    continue;
                }
                else
                {
                    // If this file isn't present in the most recent commit, then it's of no interest
                    return null;
                }
            }

            checkedFirstCommit = true;
            var parents = currentCommit.Parents;
            var count = parents.Count();
            if (count == 0)
            {
                return new CommitWrapper(currentCommit);
            }
            else if (count > 1)
            {
                // Multiple parents means a merge. Ignore.
                continue;
            }
            else
            {
                var parentTree = parents.First();
                var parentTreeEntry = parentTree[relativePath];
                if (parentTreeEntry == null || parentTreeEntry.Target.Id != currentTreeEntry.Target.Id)
                {
                    return new CommitWrapper(currentCommit);
                }
            }
        }

        return null;
    }
}
