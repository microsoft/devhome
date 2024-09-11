// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation.Collections;

namespace FileExplorerGitIntegration.Models;

[ComVisible(false)]
[ClassInterface(ClassInterfaceType.None)]
public sealed class GitLocalRepository : ILocalRepository
{
    private readonly RepositoryCache? _repositoryCache;

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(GitLocalRepository));

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
        RootFolder = rootFolder;
        _repositoryCache = cache;

        try
        {
            // Rather than open the repo from scratch as validation, try to retrieve it from the cache
            OpenRepository();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Exception thrown in OpenRepository");
            throw;
        }
    }

    private RepositoryWrapper OpenRepository()
    {
        if (_repositoryCache != null)
        {
            return _repositoryCache.GetRepository(RootFolder);
        }

        return new RepositoryWrapper(RootFolder);
    }

    IPropertySet ILocalRepository.GetProperties(string[] properties, string relativePath)
    {
        relativePath = relativePath.Replace('\\', '/');
        var result = new ValueSet();
        CommitWrapper? latestCommit = null;

        var repository = OpenRepository();

        if (repository is null)
        {
            _log.Debug("GetProperties: Repository object is null");
            return result;
        }

        foreach (var propName in properties)
        {
            switch (propName)
            {
                case "System.VersionControl.LastChangeMessage":
                    latestCommit ??= FindLatestCommit(relativePath, repository);
                    if (latestCommit is not null)
                    {
                        result.Add(propName, latestCommit.MessageShort);
                    }

                    break;
                case "System.VersionControl.LastChangeAuthorName":
                    latestCommit ??= FindLatestCommit(relativePath, repository);
                    if (latestCommit is not null)
                    {
                        result.Add(propName, latestCommit.AuthorName);
                    }

                    break;
                case "System.VersionControl.LastChangeDate":
                    latestCommit ??= FindLatestCommit(relativePath, repository);
                    if (latestCommit is not null)
                    {
                        result.Add(propName, latestCommit.AuthorWhen);
                    }

                    break;
                case "System.VersionControl.LastChangeAuthorEmail":
                    latestCommit ??= FindLatestCommit(relativePath, repository);
                    if (latestCommit is not null)
                    {
                        result.Add(propName, latestCommit.AuthorEmail);
                    }

                    break;
                case "System.VersionControl.LastChangeID":
                    latestCommit ??= FindLatestCommit(relativePath, repository);
                    if (latestCommit is not null)
                    {
                        result.Add(propName, latestCommit.Sha);
                    }

                    break;
                case "System.VersionControl.Status":
                    result.Add(propName, GetStatus(relativePath, repository));
                    break;

                case "System.VersionControl.CurrentFolderStatus":
                    var folderStatus = GetFolderStatus(relativePath, repository);
                    if (folderStatus is not null)
                    {
                        result.Add(propName, folderStatus);
                    }

                    break;
            }
        }

        _log.Debug("Returning source control properties from git source control extension");
        return result;
    }

    public IPropertySet GetProperties(string[] properties, string relativePath)
    {
        return ((ILocalRepository)this).GetProperties(properties, relativePath);
    }

    private string? GetFolderStatus(string relativePath, RepositoryWrapper repository)
    {
        try
        {
            return repository.GetRepoStatus();
        }
        catch
        {
            return null;
        }
    }

    private string? GetStatus(string relativePath, RepositoryWrapper repository)
    {
        try
        {
            return repository.GetFileStatus(relativePath);
        }
        catch
        {
            return null;
        }
    }

    private CommitWrapper? FindLatestCommit(string relativePath, RepositoryWrapper repository)
    {
        return repository.FindLastCommit(relativePath);
    }
}
