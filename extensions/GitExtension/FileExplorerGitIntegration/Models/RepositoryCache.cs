// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Diagnostics;
using LibGit2Sharp;
using Serilog;

namespace FileExplorerGitIntegration.Models;

internal sealed class RepositoryCache : IDisposable
{
    private readonly ConcurrentDictionary<string, Repository> _repositoryCache = new();
    private readonly ConcurrentDictionary<(Repository repo, Commit head), CommitLogCache> _logCache = new();
    private readonly Serilog.ILogger log = Log.ForContext("SourceContext", nameof(RepositoryCache));
    private bool disposedValue;

    public Repository GetRepository(string rootFolder)
    {
        if (_repositoryCache.TryGetValue(rootFolder, out Repository? repo))
        {
            return repo;
        }

        var tempRepo = new Repository(rootFolder);
        try
        {
            if (!_repositoryCache.TryAdd(rootFolder, tempRepo))
            {
                // Another thread beat us here. Dispose of the repo we just created and get the one from the cache.
                tempRepo.Dispose();
                var result = _repositoryCache.TryGetValue(rootFolder, out repo);
                Debug.Assert(result, "Failed to get newly added repo from cache");
                Debug.Assert(repo != null, "Repo from cache should not be null");
            }
            else
            {
                repo = tempRepo;
                tempRepo = null;
            }
        }
        finally
        {
            if (tempRepo != null)
            {
                tempRepo.Dispose();
            }
        }

        return repo;
    }

    public CommitLogCache GetCommitLog(Repository repo)
    {
        var head = repo.Head.Tip;
        var key = (repo, head);
        if (_logCache.TryGetValue(key, out CommitLogCache? cache))
        {
            return cache;
        }

        cache = new CommitLogCache(repo);
        if (!_logCache.TryAdd(key, cache))
        {
            // Another thread beat us here. Dispose of the CommitLogCache we just created and get the one from the cache.
            var result = _logCache.TryGetValue(key, out cache);
            Debug.Assert(result, "Failed to get newly added CommitLogCache from cache");
            Debug.Assert(cache != null, "CommitLogCache from cache should not be null");
        }

        return cache;
    }

    internal void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                foreach (var repo in _repositoryCache.Values)
                {
                    repo.Dispose();
                }
            }

            _repositoryCache.Clear();
            disposedValue = true;
        }
    }

    void IDisposable.Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
