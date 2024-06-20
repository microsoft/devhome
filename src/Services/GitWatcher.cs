// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using DevHome.Contracts.Services;

namespace DevHome.Services;

public class GitWatcher : IGitWatcher
{
    private static readonly Lazy<GitWatcher> _lazyInstance = new(() => new());

    public static GitWatcher Instance => _lazyInstance.Value;

    public event EventHandler<GitRepositoryChangedEventArgs>? GitRepositoryCreated;

    public event EventHandler<GitRepositoryChangedEventArgs>? GitRepositoryDeleted;

    private readonly Dictionary<string, FileSystemWatcher> _newRepoWatchers;
    private readonly Dictionary<string, FileSystemWatcher> _existingRepoWatchers;

    // Used to protect two dictionaries above from simultaneous modification
    private readonly object _modificationLock = new();

    // Checks for one of the following formats of drive roots (must be a full string match):
    // c:
    // D:\
    // \\?\X:
    private static readonly Regex _rootIsDrive = new(@"^(([a-z]:\\?)|(\\\\\?\\[a-z]:))$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private GitWatcher()
    {
        // TODO: Add rehydration logic either here or in core app initialization code
        // https://github.com/microsoft/devhome/issues/618
        _newRepoWatchers = new();
        _existingRepoWatchers = new();
    }

    public void AddTrackedRepositories(Collection<string> repositoryPaths)
    {
        lock (_modificationLock)
        {
            repositoryPaths.ToList().ForEach(
                (repositoryPath) =>
                {
                    // Path must be fully qualified to a root drive, i.e. not a UNC path
                    if (!Path.IsPathFullyQualified(repositoryPath) ||
                        (repositoryPath.IndexOfAny(Path.GetInvalidPathChars()) != -1) ||
                        !_rootIsDrive.IsMatch(Path.GetPathRoot(repositoryPath)!))
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
        lock (_modificationLock)
        {
            repositoryPaths.ToList().ForEach(
                (repositoryPath) =>
                {
                    var path = repositoryPath.ToLower(CultureInfo.InvariantCulture);
                    _existingRepoWatchers.Remove(path);
                });
        }
    }

    public List<string> GetTrackedRepositories() => _existingRepoWatchers.Keys.ToList();

    public void SetMonitoredSources(Collection<string>? sources, bool append = false)
    {
        lock (_modificationLock)
        {
            if (!append)
            {
                _newRepoWatchers.Clear();
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

                    _newRepoWatchers.Add(source, watcher);
                });
        }
    }

    public bool RemoveMonitoredSource(string source)
    {
        lock (_modificationLock)
        {
            return _newRepoWatchers.Remove(source);
        }
    }

    public List<string> GetMonitoredSources() => _newRepoWatchers.Keys.ToList();

    public IGitFileWatcher CreateFileWatcher(string filePattern)
    {
        return new GitFileWatcher(this, filePattern);
    }

    private void AddNewRepoWatcher(string path)
    {
        lock (_modificationLock)
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

            _existingRepoWatchers.Add(path, deletionWatcher);
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
        lock (_modificationLock)
        {
            if (!e.FullPath.Contains(@"\.git\"))
            {
                return;
            }

            var path = GetRepoRootFromFileInGitFolder(e.FullPath);

            // TODO: Linux filesystems may recognize upper- and lowercase paths as distinct
            // https://github.com/microsoft/devhome/issues/620
            path = path.ToLower(CultureInfo.InvariantCulture);
            if (!_existingRepoWatchers.ContainsKey(path))
            {
                AddNewRepoWatcher(path);
                GitRepositoryCreated?.Invoke(this, new GitRepositoryChangedEventArgs(GitRepositoryChangeType.Created, path));
            }
        }
    }

    private void RepoSentinelFileDeleted(object sender, FileSystemEventArgs e)
    {
        lock (_modificationLock)
        {
            var path = GetRepoRootFromFileInGitFolder(e.FullPath);

            path = path.ToLower(CultureInfo.InvariantCulture);
            if (_existingRepoWatchers.Remove(path))
            {
                GitRepositoryDeleted?.Invoke(this, new GitRepositoryChangedEventArgs(GitRepositoryChangeType.Deleted, path));
            }
        }
    }
}

public class GitFileWatcher : IGitFileWatcher
{
    private readonly Dictionary<string, FileSystemWatcher> _watchers;
    private readonly GitWatcher _owner;

    // Used to protect "watchers" from simultaneous modification
    private readonly object _modificationLock = new();

    public bool IsOpen { get; private set; }

    public string Filter { get; }

    public event EventHandler<GitFileChangedEventArgs>? FileCreated;

    public event EventHandler<GitFileChangedEventArgs>? FileModified;

    public event EventHandler<GitFileChangedEventArgs>? FileDeleted;

    public GitFileWatcher(GitWatcher owner, string filePattern)
    {
        this._owner = owner;
        Filter = filePattern;

        _watchers = new();

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

        lock (_modificationLock)
        {
            var key = repository.ToLower(CultureInfo.InvariantCulture);
            if (_watchers.TryAdd(key, watcher))
            {
                watcher.EnableRaisingEvents = true;
            }
        }
    }

    private void WatchedFileChanged(object sender, FileSystemEventArgs e)
    {
        foreach (var watcher in _watchers)
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
        foreach (var watcher in _watchers)
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
        foreach (var watcher in _watchers)
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

        _owner.GitRepositoryCreated -= OnRepoCreated;
        _owner.GitRepositoryDeleted -= OnRepoDeleted;

        _watchers.Clear();
    }

    private void OnRepoCreated(object? sender, GitRepositoryChangedEventArgs e)
    {
        CreateWatcher(Filter, e.RepositoryPath);
    }

    private void OnRepoDeleted(object? sender, GitRepositoryChangedEventArgs e)
    {
        lock (_modificationLock)
        {
            _watchers.Remove(Filter);
        }
    }

    ~GitFileWatcher()
    {
        if (IsOpen)
        {
            _owner.GitRepositoryCreated -= OnRepoCreated;
            _owner.GitRepositoryDeleted -= OnRepoDeleted;
        }
    }
}
