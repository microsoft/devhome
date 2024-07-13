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
    private readonly Serilog.ILogger log = Log.ForContext("SourceContext", nameof(RepositoryCache));
    private bool disposedValue;

    public Repository? GetRepository(string rootFolder)
    {
        if (_repositoryCache.TryGetValue(rootFolder, out Repository? repo))
        {
            return repo;
        }

        try
        {
            repo = new Repository(rootFolder);
            if (!_repositoryCache.TryAdd(rootFolder, repo))
            {
                // Another thread beat us here. Dispose of the repo we just created and get the one from the cache.
                repo.Dispose();
                var result = _repositoryCache.TryGetValue(rootFolder, out repo);
                Debug.Assert(result, "Failed to get newly added repo from cache");
                Debug.Assert(repo != null, "Repo from cache should not be null");
            }

            return repo;
        }
        catch (Exception ex)
        {
            log.Error("RepositoryCache", "Failed to create Repository", ex);
            if (repo != null)
            {
                repo.Dispose();
            }

            return null;
        }
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
