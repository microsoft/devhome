// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using LibGit2Sharp;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace FileExplorerGitIntegration.Models;

internal sealed class RepositoryWrapper : IDisposable
{
    private readonly Repository _repo;
    private readonly ReaderWriterLockSlim _repoLock = new();

    private readonly string _workingDirectory;

    private Commit? _head;
    private CommitLogCache? _commits;

    private bool _disposedValue;

    private GitDetect _gitDetect = new();
    private bool _gitInstalled;

    public RepositoryWrapper(string rootFolder)
    {
        _repo = new Repository(rootFolder);
        _workingDirectory = _repo.Info.WorkingDirectory;
        _gitInstalled = _gitDetect.DetectGit();
    }

    public IEnumerable<Commit> GetCommits()
    {
        // Fast path: if we have an up-to-date commit log, return that
        if (_head != null && _commits != null)
        {
            _repoLock.EnterReadLock();
            try
            {
                if (_repo.Head.Tip == _head)
                {
                    return _commits;
                }
            }
            finally
            {
                _repoLock?.ExitReadLock();
            }
        }

        // Either the commit log hasn't been created yet, or it's out of date
        _repoLock.EnterWriteLock();
        try
        {
            if (_head == null || _commits == null || _repo.Head.Tip != _head)
            {
                _commits = new CommitLogCache(_repo);
                _head = _repo.Head.Tip;
            }
        }
        finally
        {
            _repoLock.ExitWriteLock();
        }

        return _commits;
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal sealed class GitStatusEntry
    {
        public GitStatusEntry(string path, FileStatus status, string? renameOldPath = null)
        {
            Path = path;
            Status = status;
            RenameOldPath = renameOldPath;
        }

        public string Path { get; set; }

        public FileStatus Status { get; set; }

        public string? RenameOldPath { get; set; }

        public string? RenameNewPath { get; set; }

        private string DebuggerDisplay
        {
            get
            {
                if (Status.HasFlag(FileStatus.RenamedInIndex) || Status.HasFlag(FileStatus.RenamedInWorkdir))
                {
                    return string.Format(CultureInfo.InvariantCulture, "{0}: {1} -> {2}", Status, RenameOldPath, Path);
                }

                return string.Format(CultureInfo.InvariantCulture, "{0}: {1}", Status, Path);
            }
        }
    }

    internal sealed class GitRepositoryStatus
    {
        private readonly Dictionary<string, GitStatusEntry> _entries = new();
        private readonly List<GitStatusEntry> _added = new();
        private readonly List<GitStatusEntry> _staged = new();
        private readonly List<GitStatusEntry> _removed = new();
        private readonly List<GitStatusEntry> _untracked = new();
        private readonly List<GitStatusEntry> _modified = new();
        private readonly List<GitStatusEntry> _missing = new();
        private readonly List<GitStatusEntry> _ignored = new();
        private readonly List<GitStatusEntry> _renamedInIndex = new();
        private readonly List<GitStatusEntry> _renamedInWorkDir = new();
        private readonly List<GitStatusEntry> _conflicted = new();

        public GitRepositoryStatus()
        {
        }

        public void Add(string path, GitStatusEntry status)
        {
            _entries.Add(path, status);
            if (status.Status.HasFlag(FileStatus.NewInIndex))
            {
                _added.Add(status);
            }

            if (status.Status.HasFlag(FileStatus.ModifiedInIndex))
            {
                _staged.Add(status);
            }

            if (status.Status.HasFlag(FileStatus.DeletedFromIndex))
            {
                _removed.Add(status);
            }

            if (status.Status.HasFlag(FileStatus.NewInWorkdir))
            {
                _untracked.Add(status);
            }

            if (status.Status.HasFlag(FileStatus.ModifiedInWorkdir))
            {
                _modified.Add(status);
            }

            if (status.Status.HasFlag(FileStatus.DeletedFromWorkdir))
            {
                _missing.Add(status);
            }

            if (status.Status.HasFlag(FileStatus.RenamedInIndex))
            {
                _renamedInIndex.Add(status);
            }

            if (status.Status.HasFlag(FileStatus.RenamedInWorkdir))
            {
                _renamedInWorkDir.Add(status);
            }

            if (status.Status.HasFlag(FileStatus.Conflicted))
            {
                _conflicted.Add(status);
            }
        }

        public Dictionary<string, GitStatusEntry> Entries => _entries;

        public List<GitStatusEntry> Added => _added;

        public List<GitStatusEntry> Staged => _staged;

        public List<GitStatusEntry> Removed => _removed;

        public List<GitStatusEntry> Untracked => _untracked;

        public List<GitStatusEntry> Modified => _modified;

        public List<GitStatusEntry> Missing => _missing;

        public List<GitStatusEntry> RenamedInIndex => _renamedInIndex;

        public List<GitStatusEntry> RenamedInWorkDir => _renamedInWorkDir;

        public List<GitStatusEntry> Conflicted => _conflicted;
    }

    public string GetRepoStatus()
    {
        var repoStatus = new GitRepositoryStatus();

        if (_gitInstalled)
        {
            var result = GitExecute.ExecuteGitCommand(_gitDetect.GitConfiguration.ReadInstallPath(), _workingDirectory, "--no-optional-locks status --porcelain=v2 -z");
            if (result.Status == ProviderOperationStatus.Success && result.Output != null)
            {
                var parts = result.Output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < parts.Length; ++i)
                {
                    var line = parts[i];
                    if (line.StartsWith("1 ", StringComparison.Ordinal))
                    {
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
                        var pieces = line.Split(' ', 11);
                        var filePath = pieces[10];
                        repoStatus.Add(filePath, new GitStatusEntry(filePath, FileStatus.Conflicted));
                    }
                    else if (line.StartsWith("? ", StringComparison.Ordinal))
                    {
                        var filePath = line.Substring(2);
                        repoStatus.Add(filePath, new GitStatusEntry(filePath, FileStatus.NewInWorkdir));
                    }
                }
            }
        }

        string branchName;
        var branchStatus = string.Empty;
        try
        {
            _repoLock.EnterWriteLock();
            branchName = _repo.Info.IsHeadDetached ?
                "Detached: " + _repo.Head.Tip.Sha[..7] :
                "Branch: " + _repo.Head.FriendlyName;
            if (_repo.Head.IsTracking)
            {
                var behind = _repo.Head.TrackingDetails.BehindBy;
                var ahead = _repo.Head.TrackingDetails.AheadBy;
                if (behind == 0 && ahead == 0)
                {
                    branchStatus = " ≡";
                }
                else if (behind > 0 && ahead > 0)
                {
                    branchStatus = " ↓" + behind + " ↑" + ahead;
                }
                else if (behind > 0)
                {
                    branchStatus = " ↓" + behind;
                }
                else if (ahead > 0)
                {
                    branchStatus = " ↑" + ahead;
                }
            }
        }
        finally
        {
            _repoLock.ExitWriteLock();
        }

        var fileStatus = $"| +{repoStatus.Added.Count} ~{repoStatus.Staged.Count} -{repoStatus.Removed.Count} | +{repoStatus.Untracked.Count} ~{repoStatus.Modified.Count} -{repoStatus.Missing.Count}";
        var conflicted = repoStatus.Conflicted.Count;

        if (conflicted > 0)
        {
            fileStatus += $" !{conflicted}";
        }

        return branchName + branchStatus + " " + fileStatus;
    }

    public string GetFileStatus(string relativePath)
    {
        // Skip directories while we're getting individual file status.
        if (File.GetAttributes(Path.Combine(_workingDirectory, relativePath)).HasFlag(FileAttributes.Directory))
        {
            return string.Empty;
        }

        FileStatus status;
        try
        {
            _repoLock.EnterWriteLock();
            status = _repo.RetrieveStatus(relativePath);
        }
        finally
        {
            _repoLock.ExitWriteLock();
        }

        if (status == FileStatus.Unaltered || status.HasFlag(FileStatus.Nonexistent | FileStatus.Ignored))
        {
            return string.Empty;
        }
        else if (status.HasFlag(FileStatus.Conflicted))
        {
            return "Merge conflict";
        }
        else if (status.HasFlag(FileStatus.NewInWorkdir))
        {
            return "Untracked";
        }

        var statusString = string.Empty;
        if (status.HasFlag(FileStatus.NewInIndex) || status.HasFlag(FileStatus.ModifiedInIndex) || status.HasFlag(FileStatus.RenamedInIndex) || status.HasFlag(FileStatus.TypeChangeInIndex))
        {
            statusString = "Staged";
        }

        if (status.HasFlag(FileStatus.ModifiedInWorkdir) || status.HasFlag(FileStatus.RenamedInWorkdir) || status.HasFlag(FileStatus.TypeChangeInWorkdir))
        {
            if (string.IsNullOrEmpty(statusString))
            {
                statusString = "Modified";
            }
            else
            {
                statusString += ", Modified";
            }
        }

        return statusString;
    }

    internal void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _repo.Dispose();
                _repoLock.Dispose();
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
