// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Helpers;
using DevHome.Common.Services;
using Serilog;
using Windows.Storage;

namespace DevHome.FileExplorerSourceControlIntegration.Services;

public class RepositoryTracking
{
    public RepoStoreOptions RepoStoreOptions
    {
        get; set;
    }

    public enum RepositoryChange
    {
        Added,
        Removed,
    }

    private readonly FileService fileService;

    private readonly object trackRepoLock = new();

    private Dictionary<string, string> TrackedRepositories { get; set; } = new Dictionary<string, string>();

    private readonly Serilog.ILogger log = Log.ForContext("SourceContext", nameof(RepositoryTracking));

    public event Windows.Foundation.TypedEventHandler<string, RepositoryChange>? RepositoryChanged;

    public RepositoryTracking(string? path)
    {
        if (RuntimeHelper.IsMSIX)
        {
            RepoStoreOptions = new RepoStoreOptions
            {
                RepoStoreFolderPath = ApplicationData.Current.LocalFolder.Path,
            };
            log.Debug("Repo Store for File Explorer Integration created under ApplicationData");
        }
        else
        {
            RepoStoreOptions = new RepoStoreOptions
            {
                RepoStoreFolderPath = path ?? string.Empty,
            };
        }

        fileService = new FileService();
        RestoreTrackedRepositoriesFomJson();
    }

    public void RestoreTrackedRepositoriesFomJson()
    {
        lock (trackRepoLock)
        {
            TrackedRepositories = fileService.Read<Dictionary<string, string>>(RepoStoreOptions.RepoStoreFolderPath, RepoStoreOptions.RepoStoreFileName);

            // No repositories are currently being tracked. The file will be created on first add to repository tracking.
            log.Debug("Repo store has just been created with the first registered repository root path");
            TrackedRepositories ??= new Dictionary<string, string>();
        }

        log.Information($"Repositories retrieved from Repo Store, number of registered repositories: {TrackedRepositories.Count}");
    }

    public void AddRepositoryPath(string extensionId, string rootPath)
    {
        lock (trackRepoLock)
        {
            if (!TrackedRepositories.ContainsKey(rootPath))
            {
                TrackedRepositories[rootPath] = extensionId!;
                fileService.Save(RepoStoreOptions.RepoStoreFolderPath, RepoStoreOptions.RepoStoreFileName, TrackedRepositories);
                try
                {
                    RepositoryChanged?.Invoke(extensionId, RepositoryChange.Added);
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"Added event signaling failed: ");
                }

                log.Information("Repository added to repo store");
            }
            else
            {
                log.Warning("Repository root path already registered in the repo store");
            }
        }
    }

    public void RemoveRepositoryPath(string rootPath)
    {
        lock (trackRepoLock)
        {
            TrackedRepositories.TryGetValue(rootPath, out var extensionId);
            TrackedRepositories.Remove(rootPath);
            fileService.Save(RepoStoreOptions.RepoStoreFolderPath, RepoStoreOptions.RepoStoreFileName, TrackedRepositories);
            try
            {
                RepositoryChanged?.Invoke(extensionId ??= string.Empty, RepositoryChange.Removed);
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Removed event signaling failed: ");
            }
        }

        log.Information("Repository removed from repo store");
    }

    public Dictionary<string, string> GetAllTrackedRepositories()
    {
        lock (trackRepoLock)
        {
            if (TrackedRepositories == null)
            {
                TrackedRepositories = fileService.Read<Dictionary<string, string>>(RepoStoreOptions.RepoStoreFolderPath, RepoStoreOptions.RepoStoreFileName);
                TrackedRepositories ??= new Dictionary<string, string>();
            }

            log.Information("All repositories retrieved from repo store");
            return TrackedRepositories;
        }
    }

    public string GetSourceControlProviderForRootPath(string rootPath)
    {
        lock (trackRepoLock)
        {
            if (TrackedRepositories.TryGetValue(rootPath, out var value))
            {
                log.Information("Source Control Provider returned for root path");
                return TrackedRepositories[rootPath];
            }
            else
            {
                log.Error("The root path is not registered for File Explorer Source Control Integration");
                return string.Empty;
            }
        }
    }
}
