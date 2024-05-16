// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using DevHome.Common.Services;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace FileExplorerGitIntegration.Models;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
[Guid("BDA76685-E749-4f09-8F13-C466D0802DA1")]
public class GitLocalRepositoryProviderFactory : ILocalRepositoryProvider
{
    private readonly RepositoryCache? _repositoryCache;

    public string DisplayName => "GitLocalRepositoryProviderFactory";

    private readonly StringResource stringResource = new("FileExplorerGitIntegration.pri", "Resources");
    private readonly string _errorResourceKey = "GetRepositoryError";

    GetLocalRepositoryResult ILocalRepositoryProvider.GetRepository(string rootPath)
    {
        try
        {
            return new GetLocalRepositoryResult(new GitLocalRepository(rootPath, _repositoryCache));
        }
        catch (Exception ex)
        {
            var log = Log.ForContext("SourceContext", nameof(GitLocalRepositoryProviderFactory));
            log.Error("GitLocalRepositoryProviderFactory", "Failed to create GitLocalRepository", ex);
            return new GetLocalRepositoryResult(ex, stringResource.GetLocalized(_errorResourceKey), $"Message: {ex.Message} and HRESULT: {ex.HResult}");
        }
    }

    public GetLocalRepositoryResult GetRepository(string rootPath)
    {
        return ((ILocalRepositoryProvider)this).GetRepository(rootPath);
    }

    internal GitLocalRepositoryProviderFactory(RepositoryCache cache)
    {
        _repositoryCache = cache;
    }

    public GitLocalRepositoryProviderFactory()
    {
    }
}
