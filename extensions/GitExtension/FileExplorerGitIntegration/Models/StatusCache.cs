// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace FileExplorerGitIntegration.Models;

using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using LibGit2Sharp;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Windows.DevHome.SDK;
using Windows.Win32;

// Caches the most recently obtained repo status.
// Use FileSystemWatcher to invalidate the cache.
// File-based invalidation can come in swarms. For example, building a project, changing/pulling branches.
// To avoid flooding with status retrievals, we "debounce" the invalidations
internal sealed class StatusCache : IDisposable
{
    private readonly string _workingDirectory;
    private readonly FileSystemWatcher _watcher;
    private readonly ThrottledTask _throttledUpdate;
    private readonly ReaderWriterLockSlim _statusLock = new();
    private readonly GitDetect _gitDetect = new();
    private readonly bool _gitInstalled;

    private GitRepositoryStatus? _status;
    private bool _disposedValue;

    public StatusCache(string rootFolder)
    {
        _workingDirectory = rootFolder;
        _throttledUpdate = new ThrottledTask(
            () =>
        {
            UpdateStatus(RetrieveStatus());
        },
            TimeSpan.FromSeconds(3));

        _gitInstalled = _gitDetect.DetectGit();

        _watcher = new FileSystemWatcher(rootFolder)
        {
            NotifyFilter = NotifyFilters.CreationTime
            | NotifyFilters.DirectoryName
            | NotifyFilters.FileName
            | NotifyFilters.LastWrite
            | NotifyFilters.Size,
            IncludeSubdirectories = true,
        };
        _watcher.Error += OnError;
        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += OnRenamed;

        _watcher.EnableRaisingEvents = true;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (ShouldIgnore(e.Name))
        {
            return;
        }

        Invalidate();
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        Invalidate();
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        Invalidate();
    }

    private bool ShouldIgnore(string? relativePath)
    {
        if (relativePath == null)
        {
            return true;
        }

        var filename = Path.GetFileName(relativePath);
        if (filename == null || filename == "index.lock" || filename == ".git")
        {
            return true;
        }

        if (relativePath.StartsWith(".git", StringComparison.OrdinalIgnoreCase) && relativePath.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public GitRepositoryStatus Status
    {
        get
        {
            _statusLock.EnterReadLock();
            if (_status != null)
            {
                var result = _status;
                _statusLock.ExitReadLock();
                return result;
            }

            // Populate initial status
            _statusLock.ExitReadLock();
            _statusLock.EnterWriteLock();
            try
            {
                _status ??= RetrieveStatus();
                return _status;
            }
            finally
            {
                _statusLock.ExitWriteLock();
            }
        }
    }

    private void UpdateStatus(GitRepositoryStatus newStatus)
    {
        GitRepositoryStatus? oldStatus;
        _statusLock.EnterWriteLock();
        try
        {
            oldStatus = _status;
            _status = newStatus;
        }
        finally
        {
            _statusLock.ExitWriteLock();
        }

        // Diff old and new status to obtain a list of files to refresh to the Shell.
        if (oldStatus == null)
        {
            return;
        }

        HashSet<string> changed = [];
        foreach (var newEntry in newStatus.FileEntries)
        {
            GitStatusEntry? oldValue;
            if (oldStatus.FileEntries.TryGetValue(newEntry.Key, out oldValue))
            {
                if (newEntry.Value.Status != oldValue.Status)
                {
                    changed.Add(newEntry.Key);
                }

                oldStatus.FileEntries.Remove(newEntry.Key);
            }
            else
            {
                changed.Add(newEntry.Key);
            }
        }

        foreach (var oldEntry in oldStatus.FileEntries)
        {
            changed.Add(oldEntry.Key);
        }

        foreach (var entry in changed)
        {
            var fixedPath = Path.Combine(_workingDirectory, entry).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            unsafe
            {
                IntPtr strPtr = Marshal.StringToCoTaskMemUni(fixedPath);
                PInvoke.SHChangeNotify(Windows.Win32.UI.Shell.SHCNE_ID.SHCNE_UPDATEITEM, Windows.Win32.UI.Shell.SHCNF_FLAGS.SHCNF_PATH, (void*)strPtr, null);
                Marshal.FreeCoTaskMem(strPtr);
            }
        }
    }

    private GitRepositoryStatus RetrieveStatus()
    {
        var repoStatus = new GitRepositoryStatus();
        if (!_gitInstalled)
        {
            return repoStatus;
        }

        // Options fully explained at https://git-scm.com/docs/git-status
        // --no-optional-locks : Since this we are essentially running in the background, don't take any optional git locks
        //                       that could interfere with the user's work. This means calling "status" won't auto-update the
        //                       index to make future "status" calls faster, but it's better to be unintrusive.
        // --porcelain=v2      : The v2 gives us nice detailed entries that help us separate ordinary changes from renames, conflicts, and untracked
        //                       Disclaimer: I'm not sure how far back porcelain=v2 is supported, but I'm pretty sure it's at least 3-4 years.
        //                       There could be old Git installations that predate it.
        // -z                  : Terminate filenames and entries with NUL instead of space/LF. This helps us deal with filenames containing spaces.
        var result = GitExecute.ExecuteGitCommand(_gitDetect.GitConfiguration.ReadInstallPath(), _workingDirectory, "--no-optional-locks status --porcelain=v2 -z");
        if (result.Status != ProviderOperationStatus.Success || result.Output == null)
        {
            return repoStatus;
        }

        var parts = result.Output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; ++i)
        {
            var line = parts[i];
            if (line.StartsWith("1 ", StringComparison.Ordinal))
            {
                // For porcelain=v2, "ordinary" entries have the following format:
                //   1 <XY> <sub> <mH> <mI> <mW> <hH> <hI> <path>
                // For now, we only care about the <XY> and <path> fields.
                var pieces = line.Split(' ', 9);
                var fileStatusString = pieces[1];
                var filePath = pieces[8];
                FileStatus statusEntry = FileStatus.Unaltered;
                switch (fileStatusString[0])
                {
                    case 'M':
                        statusEntry |= FileStatus.ModifiedInIndex;
                        break;

                    case 'T':
                        statusEntry |= FileStatus.TypeChangeInIndex;
                        break;

                    case 'A':
                        statusEntry |= FileStatus.NewInIndex;
                        break;

                    case 'D':
                        statusEntry |= FileStatus.DeletedFromIndex;
                        break;
                }

                switch (fileStatusString[1])
                {
                    case 'M':
                        statusEntry |= FileStatus.ModifiedInWorkdir;
                        break;

                    case 'T':
                        statusEntry |= FileStatus.TypeChangeInWorkdir;
                        break;

                    case 'A':
                        statusEntry |= FileStatus.NewInWorkdir;
                        break;

                    case 'D':
                        statusEntry |= FileStatus.DeletedFromWorkdir;
                        break;
                }

                repoStatus.Add(filePath, new GitStatusEntry(filePath, statusEntry));
            }
            else if (line.StartsWith("2 ", StringComparison.Ordinal))
            {
                // For porcelain=v2, "rename" entries have the following format:
                //   2 <XY> <sub> <mH> <mI> <mW> <hH> <hI> <X><score> <path><sep><origPath>
                // For now, we only care about the <XY>, <path>, and <origPath> fields.
                var pieces = line.Split(' ', 9);
                var fileStatusString = pieces[1];
                var newPath = pieces[8];
                var oldPath = parts[++i];
                FileStatus statusEntry = FileStatus.Unaltered;
                if (fileStatusString[0] == 'R')
                {
                    statusEntry |= FileStatus.RenamedInIndex;
                }

                if (fileStatusString[1] == 'R')
                {
                    statusEntry |= FileStatus.RenamedInWorkdir;
                }

                repoStatus.Add(newPath, new GitStatusEntry(newPath, statusEntry, oldPath));
            }
            else if (line.StartsWith("u ", StringComparison.Ordinal))
            {
                // For porcelain=v2, "unmerged" entries have the following format:
                //   u <XY> <sub> <m1> <m2> <m3> <mW> <h1> <h2> <h3> <path>
                // For now, we only care about the <path>. (We only say that the file has a conflict, not the details)
                var pieces = line.Split(' ', 11);
                var filePath = pieces[10];
                repoStatus.Add(filePath, new GitStatusEntry(filePath, FileStatus.Conflicted));
            }
            else if (line.StartsWith("? ", StringComparison.Ordinal))
            {
                // For porcelain=v2, "untracked" entries have the following format:
                //   ? <path>
                // For now, we only care about the <path>.
                var filePath = line.Substring(2);
                repoStatus.Add(filePath, new GitStatusEntry(filePath, FileStatus.NewInWorkdir));
            }
        }

        return repoStatus;
    }

    private void Invalidate()
    {
        _throttledUpdate.Run();
    }

    internal void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _watcher.Dispose();
                _statusLock.Dispose();
            }
        }

        _disposedValue = true;
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
