// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
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

    private readonly GitDetect _gitDetect = new();
    private readonly bool _gitInstalled;

    private Commit? _head;
    private CommitLogCache? _commits;

    private bool _disposedValue;

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

    public string GetRepoStatus()
    {
        var repoStatus = new GitRepositoryStatus();


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

        var fileStatus = $"| +{repoStatus.Added.Count} ~{repoStatus.Staged.Count + repoStatus.RenamedInIndex.Count} -{repoStatus.Removed.Count} | +{repoStatus.Untracked.Count} ~{repoStatus.Modified.Count + repoStatus.RenamedInWorkDir.Count} -{repoStatus.Missing.Count}";
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
