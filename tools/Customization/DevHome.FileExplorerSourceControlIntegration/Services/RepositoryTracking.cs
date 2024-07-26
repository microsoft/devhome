// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.SourceControlIntegration;
using DevHome.Telemetry;
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

    private readonly FileService _fileService;

    private readonly object _trackRepoLock = new();

    private Dictionary<string, string> TrackedRepositories { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly Serilog.ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryTracking));

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
            _log.Debug("Repo Store for File Explorer Integration created under ApplicationData");
        }
        else
        {
            RepoStoreOptions = new RepoStoreOptions
            {
                RepoStoreFolderPath = path ?? string.Empty,
            };
        }

        _fileService = new FileService();
        RestoreTrackedRepositoriesFomJson();
    }

    public void RestoreTrackedRepositoriesFomJson()
    {
        lock (_trackRepoLock)
        {
            var caseSensitiveDictionary = _fileService.Read<Dictionary<string, string>>(RepoStoreOptions.RepoStoreFolderPath, RepoStoreOptions.RepoStoreFileName);

            // No repositories are currently being tracked. The file will be created on first add to repository tracking.
            if (caseSensitiveDictionary == null)
            {
                TrackedRepositories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _log.Debug("Repo store cache has just been created");
            }
            else
            {
                TrackedRepositories = caseSensitiveDictionary.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
            }

            LastRestore = DateTime.Now;
        }

        _log.Information($"Repositories retrieved from Repo Store, number of registered repositories: {TrackedRepositories.Count}");
    }

    public void AddRepositoryPath(string extensionCLSID, string rootPath)
    {
        lock (_trackRepoLock)
        {
            if (!TrackedRepositories.ContainsKey(rootPath))
            {
                TrackedRepositories[rootPath] = extensionCLSID!;
                _fileService.Save(RepoStoreOptions.RepoStoreFolderPath, RepoStoreOptions.RepoStoreFileName, TrackedRepositories);
                _log.Information("Repository added to repo store");
                try
                {
                    RepositoryChanged?.Invoke(extensionCLSID, RepositoryChange.Added);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Added event signaling failed: ");
                }
            }
            else
            {
                _log.Warning("Repository root path already registered in the repo store");
            }
        }

        TelemetryFactory.Get<ITelemetry>().Log("AddEnhancedRepository_Event", LogLevel.Critical, new SourceControlIntegrationEvent(extensionCLSID, rootPath, TrackedRepositories.Count));
    }

    public void RemoveRepositoryPath(string rootPath)
    {
        var extensionCLSID = string.Empty;
        lock (_trackRepoLock)
        {
            TrackedRepositories.TryGetValue(rootPath, out extensionCLSID);
            TrackedRepositories.Remove(rootPath);
            _fileService.Save(RepoStoreOptions.RepoStoreFolderPath, RepoStoreOptions.RepoStoreFileName, TrackedRepositories);
            _log.Information("Repository removed from repo store");
            try
            {
                RepositoryChanged?.Invoke(extensionCLSID ??= string.Empty, RepositoryChange.Removed);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Removed event signaling failed: ");
            }
        }

        TelemetryFactory.Get<ITelemetry>().Log("RemoveEnhancedRepository_Event", LogLevel.Critical, new SourceControlIntegrationEvent(extensionCLSID ?? string.Empty, rootPath, TrackedRepositories.Count));
    }

    public Dictionary<string, string> GetAllTrackedRepositories()
    {
        lock (_trackRepoLock)
        {
            ReloadRepositoryStoreIfChangesDetected();
            _log.Information("All repositories retrieved from repo store");
            return TrackedRepositories;
        }
    }

    public string GetSourceControlProviderForRootPath(string rootPath)
    {
        lock (_trackRepoLock)
        {
            ReloadRepositoryStoreIfChangesDetected();
            if (TrackedRepositories.TryGetValue(rootPath, out var value))
            {
                _log.Information("Source Control Provider returned for root path");
                return TrackedRepositories[rootPath];
            }
            else
            {
                _log.Error("The root path is not registered for File Explorer Source Control Integration");
                return string.Empty;
            }
        }
    }

    public void ModifySourceControlProviderForTrackedRepository(string extensionCLSID, string rootPath)
    {
        lock (trackRepoLock)
        {
            if (TrackedRepositories.TryGetValue(rootPath, out var existingExtensionCLSID))
            {
                TrackedRepositories[rootPath] = extensionCLSID;
                fileService.Save(RepoStoreOptions.RepoStoreFolderPath, RepoStoreOptions.RepoStoreFileName, TrackedRepositories);
                log.Information("Source control extension for tracked repository modified");
            }
            else
            {
                log.Error("The root path is not registered for File Explorer Source Control Integration");
            }
        }
    }

    public void ReloadRepositoryStoreIfChangesDetected()
    {
        var lastTimeModified = System.IO.File.GetLastWriteTime(Path.Combine(RepoStoreOptions.RepoStoreFolderPath, RepoStoreOptions.RepoStoreFileName));
        _log.Information("Last Time Modified: {0}", lastTimeModified);
        if (DateTime.Compare(LastRestore, lastTimeModified) < 0)
        {
            RestoreTrackedRepositoriesFomJson();
            _log.Information("Tracked repositories restored from JSON at {0}", DateTime.Now);
        }
    }
}
