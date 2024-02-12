// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using DevHome.Contracts.Services;

namespace DevHome.Services;

public class GitWatcher : IGitWatcher
{
    private static readonly Lazy<GitWatcher> LazyInstance = new(() => new());

    public static GitWatcher Instance => LazyInstance.Value;

    public event EventHandler<GitRepositoryChangedEventArgs>? GitRepositoryCreated;

    public event EventHandler<GitRepositoryChangedEventArgs>? GitRepositoryDeleted;

    private readonly Dictionary<string, FileSystemWatcher> newRepoWatchers;
    private readonly Dictionary<string, FileSystemWatcher> existingRepoWatchers;

    // Used to protect two dictionaries above from simultaneous modification
    private readonly object modificationLock = new();

    // Checks for one of the following formats of drive roots (must be a full string match):
    // c:
    // D:\
    // \\?\X:
    private static readonly Regex RootIsDrive = new(@"^(([a-z]:\\?)|(\\\\\?\\[a-z]:))$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private GitWatcher()
    {
        // TODO: Add rehydration logic either here or in core app initialization code
        // https://github.com/microsoft/devhome/issues/618
        newRepoWatchers = new();
        existingRepoWatchers = new();
    }

    public void AddTrackedRepositories(Collection<string> repositoryPaths)
    {
        lock (modificationLock)
        {
            repositoryPaths.ToList().ForEach(
                (repositoryPath) =>
                {
                    // Path must be fully qualified to a root drive, i.e. not a UNC path
                    if (!Path.IsPathFullyQualified(repositoryPath) ||
                        (repositoryPath.IndexOfAny(Path.GetInvalidPathChars()) != -1) ||
                        !RootIsDrive.IsMatch(Path.GetPathRoot(repositoryPath)!))
                    {
                        throw new ArgumentException("Path is not fully qualified or is a UNC path.");
                    }

                    var path = repositoryPath.ToLower(CultureInfo.InvariantCulture);
                    AddNewRepoWatcher(path);
                });
        }
    }

    public void RemoveTrackedRepositories(Collection<string> repositoryPaths)
    {
        lock (modificationLock)
        {
            repositoryPaths.ToList().ForEach(
                (repositoryPath) =>
                {
                    var path = repositoryPath.ToLower(CultureInfo.InvariantCulture);
                    existingRepoWatchers.Remove(path);
                });
        }
    }

    public List<string> GetTrackedRepositories() => existingRepoWatchers.Keys.ToList();

    public void SetMonitoredSources(Collection<string>? sources, bool append = false)
    {
        lock (modificationLock)
        {
            if (!append)
            {
                newRepoWatchers.Clear();
            }

            sources?.ToList().ForEach(
                (source) =>
                {
                    // TODO: handle WSL paths
                    // https://github.com/microsoft/devhome/issues/619
                    FileSystemWatcher watcher = new(source)
                    {
                        NotifyFilter = NotifyFilters.LastWrite
                        | NotifyFilters.CreationTime
                        | NotifyFilters.FileName
                        | NotifyFilters.Size
                        | NotifyFilters.LastAccess,

                        IncludeSubdirectories = true,
                        Filter = "description",
                    };

                    watcher.Created += RepoSentinelFileCreated;

                    watcher.EnableRaisingEvents = true;

                    newRepoWatchers.Add(source, watcher);
                });
        }
    }

    public bool RemoveMonitoredSource(string source)
    {
        lock (modificationLock)
        {
            return newRepoWatchers.Remove(source);
        }
    }

    public List<string> GetMonitoredSources() => newRepoWatchers.Keys.ToList();

    public IGitFileWatcher CreateFileWatcher(string filePattern)
    {
        return new GitFileWatcher(this, filePattern);
    }

    private void AddNewRepoWatcher(string path)
    {
        lock (modificationLock)
        {
            FileSystemWatcher deletionWatcher = new(path + @"\.git")
            {
                NotifyFilter = NotifyFilters.LastWrite
                    | NotifyFilters.CreationTime
                    | NotifyFilters.FileName
                    | NotifyFilters.Size
                    | NotifyFilters.LastAccess,

                Filter = @"HEAD",
            };

            deletionWatcher.Deleted += RepoSentinelFileDeleted;

            deletionWatcher.EnableRaisingEvents = true;

            existingRepoWatchers.Add(path, deletionWatcher);
        }
    }

    // Gets the root of the repository from a path like C:\...\rootOfRepo\.git\description
    // Essentially just goes up two levels, with associated validation
    private static string GetRepoRootFromFileInGitFolder(string path)
    {
        var result = Path.GetDirectoryName(path);
        if (result == null)
        {
            throw new FileNotFoundException();
        }

        result = Path.GetDirectoryName(result);
        if (result == null)
        {
            throw new FileNotFoundException();
        }

        return result;
    }

    private void RepoSentinelFileCreated(object sender, FileSystemEventArgs e)
    {
        lock (modificationLock)
        {
            if (!e.FullPath.Contains(@"\.git\"))
            {
                return;
            }

            var path = GetRepoRootFromFileInGitFolder(e.FullPath);

            // TODO: Linux filesystems may recognize upper- and lowercase paths as distinct
            // https://github.com/microsoft/devhome/issues/620
            path = path.ToLower(CultureInfo.InvariantCulture);
            if (!existingRepoWatchers.ContainsKey(path))
            {
                AddNewRepoWatcher(path);
                GitRepositoryCreated?.Invoke(this, new GitRepositoryChangedEventArgs(GitRepositoryChangeType.Created, path));
            }
        }
    }

    private void RepoSentinelFileDeleted(object sender, FileSystemEventArgs e)
    {
        lock (modificationLock)
        {
            var path = GetRepoRootFromFileInGitFolder(e.FullPath);

            path = path.ToLower(CultureInfo.InvariantCulture);
            if (existingRepoWatchers.Remove(path))
            {
                GitRepositoryDeleted?.Invoke(this, new GitRepositoryChangedEventArgs(GitRepositoryChangeType.Deleted, path));
            }
        }
    }
}

public class GitFileWatcher : IGitFileWatcher
{
    private readonly Dictionary<string, FileSystemWatcher> watchers;
    private readonly GitWatcher owner;

    // Used to protect "watchers" from simultaneous modification
    private readonly object modificationLock = new();

    public bool IsOpen { get; private set; }

    public string Filter { get; }

    public event EventHandler<GitFileChangedEventArgs>? FileCreated;

    public event EventHandler<GitFileChangedEventArgs>? FileModified;

    public event EventHandler<GitFileChangedEventArgs>? FileDeleted;

    public GitFileWatcher(GitWatcher owner, string filePattern)
    {
        this.owner = owner;
        Filter = filePattern;

        watchers = new();

        owner.GitRepositoryCreated += OnRepoCreated;
        owner.GitRepositoryDeleted += OnRepoDeleted;

        owner.GetTrackedRepositories().ForEach((repository) => CreateWatcher(filePattern, repository));
    }

    private void CreateWatcher(string filePattern, string repository)
    {
        FileSystemWatcher watcher = new(repository)
        {
            NotifyFilter = NotifyFilters.LastWrite
                        | NotifyFilters.CreationTime
                        | NotifyFilters.FileName
                        | NotifyFilters.Size
                        | NotifyFilters.LastAccess,

            IncludeSubdirectories = true,
            Filter = filePattern,
        };

        watcher.Created += WatchedFileCreated;
        watcher.Changed += WatchedFileChanged;
        watcher.Deleted += WatchedFileDeleted;

        lock (modificationLock)
        {
            var key = repository.ToLower(CultureInfo.InvariantCulture);
            if (watchers.TryAdd(key, watcher))
            {
                watcher.EnableRaisingEvents = true;
            }
        }
    }

    private void WatchedFileChanged(object sender, FileSystemEventArgs e)
    {
        foreach (var watcher in watchers)
        {
            if (e.FullPath.StartsWith(watcher.Key, StringComparison.InvariantCultureIgnoreCase))
            {
                FileModified?.Invoke(this, new GitFileChangedEventArgs(GitFileChangeType.Modified, watcher.Key, e.FullPath));
                return;
            }
        }

        throw new DirectoryNotFoundException();
    }

    private void WatchedFileCreated(object sender, FileSystemEventArgs e)
    {
        foreach (var watcher in watchers)
        {
            if (e.FullPath.StartsWith(watcher.Key, StringComparison.InvariantCultureIgnoreCase))
            {
                FileCreated?.Invoke(this, new GitFileChangedEventArgs(GitFileChangeType.Created, watcher.Key, e.FullPath));
                return;
            }
        }

        throw new DirectoryNotFoundException();
    }

    private void WatchedFileDeleted(object sender, FileSystemEventArgs e)
    {
        foreach (var watcher in watchers)
        {
            if (e.FullPath.StartsWith(watcher.Key, StringComparison.InvariantCultureIgnoreCase))
            {
                FileDeleted?.Invoke(this, new GitFileChangedEventArgs(GitFileChangeType.Deleted, watcher.Key, e.FullPath));
                return;
            }
        }

        throw new DirectoryNotFoundException();
    }

    public void Close()
    {
        IsOpen = false;

        owner.GitRepositoryCreated -= OnRepoCreated;
        owner.GitRepositoryDeleted -= OnRepoDeleted;

        watchers.Clear();
    }

    private void OnRepoCreated(object? sender, GitRepositoryChangedEventArgs e)
    {
        CreateWatcher(Filter, e.RepositoryPath);
    }

    private void OnRepoDeleted(object? sender, GitRepositoryChangedEventArgs e)
    {
        lock (modificationLock)
        {
            watchers.Remove(Filter);
        }
    }

    ~GitFileWatcher()
    {
        if (IsOpen)
        {
            owner.GitRepositoryCreated -= OnRepoCreated;
            owner.GitRepositoryDeleted -= OnRepoDeleted;
        }
    }
}
