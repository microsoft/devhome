// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using DevHome.Common.Services;
using LibGit2Sharp;
using Microsoft.Windows.DevHome.SDK;

namespace FileExplorerGitIntegration.Models;

internal sealed class RepositoryWrapper : IDisposable
{
    private readonly GitDetect _gitDetect = new();
    private readonly ReaderWriterLockSlim _repoLock = new();

    private readonly string _workingDirectory;

    private readonly StatusCache _statusCache;

    private readonly StringResource _stringResource = new("FileExplorerGitIntegration.pri", "Resources");
    private readonly string _folderStatusBranch;
    private readonly string _folderStatusDetached;
    private readonly string _fileStatusMergeConflict;
    private readonly string _fileStatusUntracked;
    private readonly string _fileStatusStaged;
    private readonly string _fileStatusStagedRenamed;
    private readonly string _fileStatusStagedModified;
    private readonly string _fileStatusStagedRenamedModified;
    private readonly string _fileStatusModified;
    private readonly string _fileStatusRenamedModified;

    private string? _head;
    private CommitLogCache? _commits;

    private bool _disposedValue;

    public RepositoryWrapper(string rootFolder)
    {
        _gitDetect.DetectGit();
        IsValidGitRepository(rootFolder);
        _workingDirectory = string.Concat(rootFolder, Path.DirectorySeparatorChar.ToString());
        _statusCache = new StatusCache(rootFolder);

        _folderStatusBranch = _stringResource.GetLocalized("FolderStatusBranch");
        _folderStatusDetached = _stringResource.GetLocalized("FolderStatusDetached");
        _fileStatusMergeConflict = _stringResource.GetLocalized("FileStatusMergeConflict");
        _fileStatusUntracked = _stringResource.GetLocalized("FileStatusUntracked");
        _fileStatusStaged = _stringResource.GetLocalized("FileStatusStaged");
        _fileStatusStagedRenamed = _stringResource.GetLocalized("FileStatusStagedRenamed");
        _fileStatusStagedModified = _stringResource.GetLocalized("FileStatusStagedModified");
        _fileStatusStagedRenamedModified = _stringResource.GetLocalized("FileStatusStagedRenamedModified");
        _fileStatusModified = _stringResource.GetLocalized("FileStatusModified");
        _fileStatusRenamedModified = _stringResource.GetLocalized("FileStatusRenamedModified");
    }

    public void IsValidGitRepository(string rootFolder)
    {
        var validateGitRootRepo = GitExecute.ExecuteGitCommand(_gitDetect.GitConfiguration.ReadInstallPath(), rootFolder, "rev-parse --show-toplevel");
        if (validateGitRootRepo.Status != ProviderOperationStatus.Success)
        {
            throw validateGitRootRepo.Ex ?? new InvalidOperationException();
        }
        else
        {
            var output = validateGitRootRepo.Output;
            if (output is null || output.Contains("fatal: not a git repository"))
            {
                throw new ArgumentOutOfRangeException(rootFolder);
            }
            else
            {
                if (WslIntegrator.IsWSLRepo(rootFolder))
                {
                    var normalizedWorkingDirectory = WslIntegrator.GetWorkingDirectoryPath(rootFolder);
                    if (output.TrimEnd('\n') != normalizedWorkingDirectory)
                    {
                        throw new ArgumentOutOfRangeException(rootFolder);
                    }

                    return;
                }

                var normalizedRootFolderPath = rootFolder.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                normalizedRootFolderPath = string.Concat(normalizedRootFolderPath, "\n");
                if (output != normalizedRootFolderPath)
                {
                    throw new ArgumentOutOfRangeException(rootFolder);
                }
            }
        }
    }

    public CommitWrapper? FindLastCommit(string relativePath)
    {
        // Fetching the most recent status to check if the file is renamed
        // should be much less expensive than getting the most recent commit.
        // So, preemtively check for a rename here.
        var commitLog = GetCommitLogCache();
        return commitLog.FindLastCommit(GetOriginalPath(relativePath));
    }

    private CommitLogCache GetCommitLogCache()
    {
        var result = GitExecute.ExecuteGitCommand(_gitDetect.GitConfiguration.ReadInstallPath(), _workingDirectory, "rev-parse HEAD");
        if (result.Status != ProviderOperationStatus.Success)
        {
            throw result.Ex ?? new InvalidOperationException();
        }

        string? head = result.Output?.Trim();
        if (head == null)
        {
            throw new InvalidOperationException("Git command output is null.");
        }

        if (_head != null && _commits != null)
        {
            _repoLock.EnterReadLock();
            try
            {
                if (head == _head)
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
            if (_head == null || _commits == null || head != _head)
            {
                _commits = new CommitLogCache(_workingDirectory);
                _head = head;
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
        var repoStatus = _statusCache.Status;

        string branchName;
        var branchStatus = string.Empty;
        try
        {
            _repoLock.EnterWriteLock();
            branchName = repoStatus.IsHeadDetached() ?
                string.Format(CultureInfo.CurrentCulture, _folderStatusDetached, repoStatus.Sha()[..7]) :
                string.Format(CultureInfo.CurrentCulture, _folderStatusBranch, repoStatus.BranchName());
            if (repoStatus.UpstreamBranch() != string.Empty)
            {
                var behind = repoStatus.BehindBy();
                var ahead = repoStatus.AheadBy();
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
        GitStatusEntry? status;
        if (!_statusCache.Status.FileEntries.TryGetValue(relativePath, out status))
        {
            return string.Empty;
        }

        if (status.Status == FileStatus.Unaltered || status.Status.HasFlag(FileStatus.Nonexistent | FileStatus.Ignored))
        {
            return string.Empty;
        }
        else if (status.Status.HasFlag(FileStatus.Conflicted))
        {
            return _fileStatusMergeConflict;
        }
        else if (status.Status.HasFlag(FileStatus.NewInWorkdir))
        {
            return _fileStatusUntracked;
        }

        var statusString = string.Empty;
        var staged = status.Status.HasFlag(FileStatus.NewInIndex) || status.Status.HasFlag(FileStatus.ModifiedInIndex) || status.Status.HasFlag(FileStatus.RenamedInIndex) || status.Status.HasFlag(FileStatus.TypeChangeInIndex);
        var modified = status.Status.HasFlag(FileStatus.ModifiedInWorkdir) || status.Status.HasFlag(FileStatus.TypeChangeInWorkdir);
        var renamed = status.Status.HasFlag(FileStatus.RenamedInIndex) || status.Status.HasFlag(FileStatus.RenamedInWorkdir);

        if (staged)
        {
            if (renamed && modified)
            {
                statusString = _fileStatusStagedRenamedModified;
            }
            else if (renamed)
            {
                statusString = _fileStatusStagedRenamed;
            }
            else if (modified)
            {
                statusString = _fileStatusStagedModified;
            }
            else
            {
                statusString = _fileStatusStaged;
            }
        }
        else if (modified)
        {
            if (renamed)
            {
                statusString = _fileStatusRenamedModified;
            }
            else
            {
                statusString = _fileStatusModified;
            }
        }

        return statusString;
    }

    // Detect uncommitted renames and return the original path.
    // This allows us to get the commit history, because the new path doesn't exist yet.
    private string GetOriginalPath(string relativePath)
    {
        _statusCache.Status.FileEntries.TryGetValue(relativePath, out var status);
        if (status is null)
        {
            return relativePath;
        }

        if (status.Status.HasFlag(FileStatus.RenamedInIndex) || status.Status.HasFlag(FileStatus.RenamedInWorkdir))
        {
            return status.RenameOldPath ?? relativePath;
        }

        return relativePath;
    }

    internal void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
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
