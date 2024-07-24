// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using LibGit2Sharp;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation.Collections;

namespace FileExplorerGitIntegration.Models;

[ComVisible(false)]
[ClassInterface(ClassInterfaceType.None)]
public sealed class GitLocalRepository : ILocalRepository
{
    private readonly RepositoryCache? _repositoryCache;

    private readonly Serilog.ILogger log = Log.ForContext("SourceContext", nameof(GitLocalRepository));

    public string RootFolder
    {
        get; init;
    }

    public GitLocalRepository(string rootFolder)
    {
        RootFolder = rootFolder;
    }

    internal GitLocalRepository(string rootFolder, RepositoryCache? cache)
    {
        if (!Repository.IsValid(rootFolder))
        {
            throw new ArgumentException("Invalid repository path");
        }

        RootFolder = rootFolder;
        _repositoryCache = cache;
    }

    private Repository OpenRepository()
    {
        if (_repositoryCache != null)
        {
            return _repositoryCache.GetRepository(RootFolder);
        }

        return new Repository(RootFolder);
    }

    private IEnumerable<Commit> GetCommits(Repository repo)
    {
        if (_repositoryCache != null)
        {
            return _repositoryCache.GetCommitLog(repo);
        }

        return repo.Commits;
    }

    IPropertySet ILocalRepository.GetProperties(string[] properties, string relativePath)
    {
        relativePath = relativePath.Replace('\\', '/');
        var result = new ValueSet();
        Commit? latestCommit = null;

        var repository = OpenRepository();

        if (repository == null)
        {
            log.Debug("GetProperties: Repository object is null");
            return result;
        }

        foreach (var propName in properties)
        {
            switch (propName)
            {
                case "System.VersionControl.LastChangeMessage":
                    if (latestCommit == null)
                    {
                        latestCommit = FindLatestCommit(relativePath, repository);
                        if (latestCommit != null)
                        {
                            result.Add("System.VersionControl.LastChangeMessage", latestCommit.MessageShort);
                        }
                    }
                    else
                    {
                        result.Add("System.VersionControl.LastChangeMessage", latestCommit.MessageShort);
                    }

                    break;
                case "System.VersionControl.LastChangeAuthorName":
                    if (latestCommit == null)
                    {
                        latestCommit = FindLatestCommit(relativePath, repository);
                        if (latestCommit != null)
                        {
                            result.Add("System.VersionControl.LastChangeAuthorName", latestCommit.Author.Name);
                        }
                    }
                    else
                    {
                        result.Add("System.VersionControl.LastChangeAuthorName", latestCommit.Author.Name);
                    }

                    break;
                case "System.VersionControl.LastChangeDate":
                    if (latestCommit == null)
                    {
                        latestCommit = FindLatestCommit(relativePath, repository);
                        if (latestCommit != null)
                        {
                            result.Add("System.VersionControl.LastChangeDate", latestCommit.Author.When);
                        }
                    }
                    else
                    {
                        result.Add("System.VersionControl.LastChangeDate", latestCommit.Author.When);
                    }

                    break;
                case "System.VersionControl.LastChangeAuthorEmail":
                    if (latestCommit == null)
                    {
                        latestCommit = FindLatestCommit(relativePath, repository);
                        if (latestCommit != null)
                        {
                            result.Add("System.VersionControl.LastChangeAuthorEmail", latestCommit.Author.Email);
                        }
                    }
                    else
                    {
                        result.Add("System.VersionControl.LastChangeAuthorEmail", latestCommit.Author.Email);
                    }

                    break;
                case "System.VersionControl.LastChangeID":
                    if (latestCommit == null)
                    {
                        latestCommit = FindLatestCommit(relativePath, repository);
                        if (latestCommit != null)
                        {
                            result.Add("System.VersionControl.LastChangeID", latestCommit.Sha);
                        }
                    }
                    else
                    {
                        result.Add("System.VersionControl.LastChangeID", latestCommit.Sha);
                    }

                    break;
                case "System.VersionControl.Status":
                    result.Add("System.VersionControl.Status", GetStatus(relativePath, repository));
                    break;

                case "System.VersionControl.CurrentFolderStatus":
                    var folderStatus = GetFolderStatus(relativePath, repository);
                    if (folderStatus != null)
                    {
                        result.Add("System.VersionControl.CurrentFolderStatus", folderStatus);
                    }

                    break;
            }
        }

        log.Debug("Returning source control properties from git source control extension");
        return result;
    }

    public IPropertySet GetProperties(string[] properties, string relativePath)
    {
        return ((ILocalRepository)this).GetProperties(properties, relativePath);
    }

    private string? GetFolderStatus(string relativePath, Repository repository)
    {
        try
        {
            // TODO: Detect submodules
            var branchName = repository.Info.IsHeadDetached ?
                "Detached: " + repository.Head.Tip.Sha[..7] :
                "Branch: " + repository.Head.FriendlyName;

            var branchStatus = string.Empty;
            if (repository.Head.IsTracking)
            {
                var behind = repository.Head.TrackingDetails.BehindBy;
                var ahead = repository.Head.TrackingDetails.AheadBy;
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

            var repositoryStatus = repository.RetrieveStatus();
            var fileStatus = $"| +{repositoryStatus.Added.Count()} ~{repositoryStatus.Staged.Count()} -{repositoryStatus.Removed.Count()} | +{repositoryStatus.Untracked.Count()} ~{repositoryStatus.Modified.Count()} -{repositoryStatus.Missing.Count()}";
            var conflicted = 0;
            foreach (var entry in repositoryStatus)
            {
                if (entry.State.HasFlag(FileStatus.Conflicted))
                {
                    ++conflicted;
                }
            }

            if (conflicted > 0)
            {
                fileStatus += $" !{conflicted}";
            }

            return branchName + branchStatus + " " + fileStatus;
        }
        catch
        {
            return null;
        }
    }

    private string? GetStatus(string relativePath, Repository repository)
    {
        try
        {
            // Skip directories while we're getting individual file status.
            if (File.GetAttributes(Path.Combine(repository.Info.WorkingDirectory, relativePath)).HasFlag(FileAttributes.Directory))
            {
                return string.Empty;
            }

            var status = repository.RetrieveStatus(relativePath);
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

            string? statusString = null;
            if (status.HasFlag(FileStatus.NewInIndex) || status.HasFlag(FileStatus.ModifiedInIndex) || status.HasFlag(FileStatus.RenamedInIndex) || status.HasFlag(FileStatus.TypeChangeInIndex))
            {
                statusString = "Staged";
            }

            if (status.HasFlag(FileStatus.ModifiedInWorkdir) || status.HasFlag(FileStatus.RenamedInWorkdir) || status.HasFlag(FileStatus.TypeChangeInWorkdir))
            {
                if (statusString == null)
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
        catch
        {
            return null;
        }
    }

    private Commit? FindLatestCommit(string relativePath, Repository repository)
    {
        var checkedFirstCommit = false;
        foreach (var currentCommit in GetCommits(repository))
        {
            var currentTree = currentCommit.Tree;
            var currentTreeEntry = currentTree[relativePath];
            if (currentTreeEntry == null)
            {
                if (checkedFirstCommit)
                {
                    continue;
                }
                else
                {
                    // If this file isn't present in the most recent commit, then it's of no interest
                    return null;
                }
            }

            checkedFirstCommit = true;
            var parents = currentCommit.Parents;
            var count = parents.Count();
            if (count == 0)
            {
                return currentCommit;
            }
            else if (count > 1)
            {
                // Multiple parents means a merge. Ignore.
                continue;
            }
            else
            {
                var parentTree = parents.First();
                var parentTreeEntry = parentTree[relativePath];
                if (parentTreeEntry == null || parentTreeEntry.Target.Id != currentTreeEntry.Target.Id)
                {
                    return currentCommit;
                }
            }
        }

        return null;
    }
}
