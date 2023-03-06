// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.RepoConfig.Models;

/// <summary>
/// Class to contain the logic for a repository provider.
/// </summary>
internal class RepositoryProvider
{
    private readonly List<IDeveloperId> _accounts = new ();

    private readonly IDevIdProvider _devIdProvider;

    private readonly IRepositoryProvider _repositoryProvider;

    public RepositoryProvider(IPlugin provider)
    {
        _devIdProvider = provider.GetProvider(ProviderType.DevId) as IDevIdProvider;
        _repositoryProvider = provider.GetProvider(ProviderType.Repository) as IRepositoryProvider;
    }

    /// <summary>
    /// Logs the current user into this provider
    /// </summary>
    public void LogIntoProvider()
    {
        _devIdProvider.LoginNewDeveloperId();
    }

    /// <summary>
    /// Gets all the logged in accounts for this provider.
    /// </summary>
    /// <returns>A list of all accounts.  May be empty.</returns>
    public IEnumerable<IDeveloperId> GetAllLoggedInAccounts()
    {
        return _devIdProvider.GetLoggedInDeveloperIds() ?? new List<IDeveloperId>();
    }

    /// <summary>
    /// Gets all the repositories an account has for this provider.
    /// </summary>
    /// <param name="developerId">The account to search in.</param>
    /// <returns>A collection of repositories.  May be empty</returns>
    public async Task<IEnumerable<IRepository>> GetAllRepositoriesAsync(IDeveloperId developerId)
    {
        return await _repositoryProvider.GetRepositoriesAsync(developerId) ?? new List<IRepository>();
    }
}
