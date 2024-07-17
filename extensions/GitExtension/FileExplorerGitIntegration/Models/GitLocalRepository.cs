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

    private Repository? OpenRepository()
    {
        if (_repositoryCache == null)
        {
            log.Debug("Cache is null. Return new repository object");
            return new Repository(RootFolder);
        }

        log.Debug("Obtained repository from cache");
        return _repositoryCache.GetRepository(RootFolder);
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
                    var folderStatus = repository.Head.FriendlyName + " AheadBy: " + repository.Head.TrackingDetails.AheadBy + " BehindBy: " + repository.Head.TrackingDetails.BehindBy;
                    result.Add("System.VersionControl.CurrentFolderStatus", folderStatus);
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

    private string? GetStatus(string relativePath, Repository repository)
    {
        try
        {
            var status = repository.RetrieveStatus(relativePath);
            if (status == FileStatus.Unaltered)
            {
                return string.Empty;
            }

            return status.ToString();
        }
        catch
        {
            return null;
        }
    }

    private Commit? FindLatestCommit(string relativePath, Repository repository)
    {
        var commits = repository.Commits;

        // Check the most recent commit and bail if the file is not present
        {
            var firstCommit = commits.FirstOrDefault();
            if (firstCommit != null && firstCommit.Tree[relativePath] == null)
            {
                return null;
            }
        }

        foreach (var currentCommit in commits)
        {
            var currentTree = currentCommit.Tree;
            var currentTreeEntry = currentTree[relativePath];
            if (currentTreeEntry == null)
            {
                continue;
            }

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
