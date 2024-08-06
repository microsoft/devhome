// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LibGit2Sharp;

namespace FileExplorerGitIntegration.Models;

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
