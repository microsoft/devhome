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
            if (TrackedRepositories == null)
            {
                TrackedRepositories = new Dictionary<string, string>();
                log.Debug("Repo store has just been created with the first registered repository root path");
            }
        }

        log.Information($"Repositories retrieved from Repo Store, number of registered repositories: {TrackedRepositories.Count}");
    }

    public void AddRepositoryPath(string extensionCLSID, string rootPath)
    {
        lock (trackRepoLock)
        {
            if (TrackedRepositories.Keys.FirstOrDefault(key => StringComparer.OrdinalIgnoreCase.Equals(key, rootPath)) == null)
            {
                TrackedRepositories[rootPath] = extensionCLSID!;
                fileService.Save(RepoStoreOptions.RepoStoreFolderPath, RepoStoreOptions.RepoStoreFileName, TrackedRepositories);
                try
                {
                    RepositoryChanged?.Invoke(extensionCLSID, RepositoryChange.Added);
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
            var rootPathRegistered = TrackedRepositories.Keys.FirstOrDefault(key => StringComparer.OrdinalIgnoreCase.Equals(key, rootPath));
            if (rootPathRegistered != null)
            {
                TrackedRepositories.TryGetValue(rootPathRegistered, out var extensionCLSID);
                TrackedRepositories.Remove(rootPathRegistered);
                fileService.Save(RepoStoreOptions.RepoStoreFolderPath, RepoStoreOptions.RepoStoreFileName, TrackedRepositories);
                log.Information("Repository removed from repo store");
                try
                {
                    RepositoryChanged?.Invoke(extensionCLSID ??= string.Empty, RepositoryChange.Removed);
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"Removed event signaling failed: ");
                }
            }
            else
            {
                log.Error("The root path is not registered for File Explorer Source Control Integration");
            }
        }
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
            var rootPathRegistered = TrackedRepositories.Keys.FirstOrDefault(key => StringComparer.OrdinalIgnoreCase.Equals(key, rootPath));
            if (rootPathRegistered != null)
            {
                log.Information("Source Control Provider returned for root path");
                return TrackedRepositories[rootPathRegistered];
            }
            else
            {
                log.Error("The root path is not registered for File Explorer Source Control Integration");
                return string.Empty;
            }
        }
    }
}
