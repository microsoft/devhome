// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

namespace DevHome.Contracts.Services;

public interface IGitWatcher
{
    /// <summary>
    /// Provides the GitWatcher with a list of repositories to monitor.
    /// </summary>
    /// <param name="repositoryPaths">
    /// A collection of strings representing repository paths to monitor. Each should point to the root
    /// directory of the repository.
    ///
    /// These can be either absolute paths, or paths prefaced with a WSL identifier (format TBD).
    /// </param>
    public void AddTrackedRepositories(Collection<string> repositoryPaths);

    /// <summary>
    /// Removes a set of repositories from monitoring.
    /// </summary>
    /// <param name="repositoryPaths">
    /// A collection of strings representing repository paths to monitor. Each should point to the root
    /// directory of the repository.
    ///
    /// These can be either absolute paths, or paths prefaced with a WSL identifier (format TBD).
    ///
    /// Paths that are not already being tracked will be ignored.
    /// </param>
    public void RemoveTrackedRepositories(Collection<string> repositoryPaths);

    /// <summary>
    /// Get the list of repositories being monitored.
    /// </summary>
    /// <returns>A list of absolute paths, or paths prefaced with WSL identifiers (format TBD),
    /// that are currently being monitored by this watcher.</returns>
    public List<string> GetTrackedRepositories();

    /// <summary>
    /// Provides a list of sources to monitor for new Git repositories.
    ///
    /// To stop monitoring, use <code>SetMonitoredSources(null, false);</code>.
    /// </summary>
    /// <param name="sources">
    /// A collection of strings representing sources to monitor.
    ///
    /// Each string can be either an absolute path, or a WSL identifier (format TBD).
    /// </param>
    /// <param name="append">true to append new sources to the monitoring list, false to overwrite</param>
    public void SetMonitoredSources(Collection<string>? sources, bool append = false);

    /// <summary>
    /// Remove a source from monitoring for new Git repositories.
    /// </summary>
    /// <param name="source">
    /// A string representing a source being monitored.
    ///
    /// Can be either an absolute path, or a WSL identifier (format TBD).
    /// </param>
    /// <returns>true if removed; false if the source specified was not already being monitored</returns>
    public bool RemoveMonitoredSource(string source);

    /// <summary>
    /// Gets the list of sources being monitored for new repositories.
    /// </summary>
    /// <returns>A list of strings representing either absolute paths or WSL identifiers of monitored sources</returns>
    public List<string> GetMonitoredSources();

    public event EventHandler<GitRepositoryChangedEventArgs> GitRepositoryCreated;

    public event EventHandler<GitRepositoryChangedEventArgs> GitRepositoryDeleted;

    /// <summary>
    /// Subscribes to callbacks for changes to files matching a particular pattern within all known repositories.
    /// </summary>
    /// <param name="filePattern">
    /// The pattern to match. Supports asterisk and question mark wildcards; see
    /// <code>FileSystemWatcher.Filter</code>'s documentation for details:
    /// https://learn.microsoft.com/dotnet/api/system.io.filesystemwatcher.filter
    /// </param>
    /// <returns>An IGitFileWatcher watching for changes to the specified file(s)</returns>
    public IGitFileWatcher CreateFileWatcher(string filePattern);
}

public enum GitRepositoryChangeType
{
    Created,
    Deleted,
}

public class GitRepositoryChangedEventArgs : EventArgs
{
    public GitRepositoryChangedEventArgs(GitRepositoryChangeType changeType, string repositoryPath)
    {
        RepositoryPath = repositoryPath;
        ChangeType = changeType;
    }

    public string RepositoryPath { get; }

    public GitRepositoryChangeType ChangeType { get; }
}

public enum GitFileChangeType
{
    Created,
    Modified,
    Deleted,
}

public class GitFileChangedEventArgs : EventArgs
{
    public GitFileChangedEventArgs(GitFileChangeType changeType, string repositoryPath, string filePath)
    {
        RepositoryPath = repositoryPath;
        FilePath = filePath;
        ChangeType = changeType;
    }

    public string RepositoryPath { get; } = string.Empty;

    public string FilePath { get; } = string.Empty;

    public GitFileChangeType ChangeType { get; }
}

public interface IGitFileWatcher
{
    public string Filter { get; }

    public event EventHandler<GitFileChangedEventArgs>? FileCreated;

    public event EventHandler<GitFileChangedEventArgs>? FileModified;

    public event EventHandler<GitFileChangedEventArgs>? FileDeleted;

    public void Close();
}
