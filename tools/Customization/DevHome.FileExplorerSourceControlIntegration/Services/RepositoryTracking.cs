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

    private Dictionary<string, string> TrackedRepositories { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly Serilog.ILogger log = Log.ForContext("SourceContext", nameof(RepositoryTracking));

    public event Windows.Foundation.TypedEventHandler<string, RepositoryChange>? RepositoryChanged;

    public DateTime LastRestore { get; set; }

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
            Dictionary<string, string> caseSensitiveDictionary = new Dictionary<string, string>();
            caseSensitiveDictionary = fileService.Read<Dictionary<string, string>>(RepoStoreOptions.RepoStoreFolderPath, RepoStoreOptions.RepoStoreFileName);

            // No repositories are currently being tracked. The file will be created on first add to repository tracking.
            if (caseSensitiveDictionary == null)
            {
                TrackedRepositories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                log.Debug("Repo store cache has just been created");
            }
            else
            {
                TrackedRepositories = caseSensitiveDictionary.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
            }

            LastRestore = DateTime.Now;
        }

        log.Information($"Repositories retrieved from Repo Store, number of registered repositories: {TrackedRepositories.Count}");
    }

    public void AddRepositoryPath(string extensionCLSID, string rootPath)
    {
        lock (trackRepoLock)
        {
            if (!TrackedRepositories.ContainsKey(rootPath))
            {
                TrackedRepositories[rootPath] = extensionCLSID!;
                fileService.Save(RepoStoreOptions.RepoStoreFolderPath, RepoStoreOptions.RepoStoreFileName, TrackedRepositories);
                log.Information("Repository added to repo store");
                try
                {
                    RepositoryChanged?.Invoke(extensionCLSID, RepositoryChange.Added);
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"Added event signaling failed: ");
                }
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
            TrackedRepositories.TryGetValue(rootPath, out var extensionCLSID);
            TrackedRepositories.Remove(rootPath);
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
    }

    public Dictionary<string, string> GetAllTrackedRepositories()
    {
        lock (trackRepoLock)
        {
            ReloadRepositoryStoreIfChangesDetected();
            log.Information("All repositories retrieved from repo store");
            return TrackedRepositories;
        }
    }

    public string GetSourceControlProviderForRootPath(string rootPath)
    {
        lock (trackRepoLock)
        {
            ReloadRepositoryStoreIfChangesDetected();
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

    public void ReloadRepositoryStoreIfChangesDetected()
    {
        var lastTimeModified = System.IO.File.GetLastWriteTime(Path.Combine(RepoStoreOptions.RepoStoreFolderPath, RepoStoreOptions.RepoStoreFileName));
        log.Information("Last Time Modified: {0}", lastTimeModified);
        if (DateTime.Compare(LastRestore, lastTimeModified) < 0)
        {
            RestoreTrackedRepositoriesFomJson();
            log.Information("Tracked repositories restored from JSON at {0}", DateTime.Now);
        }
    }
}
