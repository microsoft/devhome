// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.RepoConfig.Models;

/// <summary>
/// A collection of all repository providers found by Dev Home.
/// </summary>
/// <remarks>
/// This class only uses providers that implement IDeveloperIdProvider and IRepositoryProvider.
/// </remarks>
internal class RepositoryProviders
{
    private readonly Dictionary<string, RepositoryProvider> _providers = new ();

    public RepositoryProviders(IEnumerable<IPlugin> providers)
    {
        foreach (var provider in providers)
        {
            if (provider.GetProvider(ProviderType.Repository) is IRepositoryProvider repositoryProvider)
            {
                this._providers.Add(repositoryProvider.GetDisplayName, new RepositoryProvider(provider));
            }
        }
    }

    /// <summary>
    /// Logs the user into a certain provider.
    /// </summary>
    /// <param name="providerName">The provider to log the user into.  Must match IRepositoryProvider.GetDisplayName</param>
    public void LogInToProvider(string providerName)
    {
        _providers.GetValueOrDefault(providerName)?.LogIntoProvider();
    }

    /// <summary>
    /// Getss the display names of all providers.
    /// </summary>
    /// <returns>A collection of display names.</returns>
    public IEnumerable<string> GetAllProviderNames()
    {
        return _providers.Keys.ToList();
    }

    /// <summary>
    /// Gets all logged in accounts for a specific provider.
    /// </summary>
    /// <param name="providerName">The provider to use.  Must match IRepositoryProvider.GetDisplayName</param>
    /// <returns>A collection of developer Ids of all logged in users.  Can be empty.</returns>
    public IEnumerable<IDeveloperId> GetAllLoggedInAccounts(string providerName)
    {
        return _providers.GetValueOrDefault(providerName)?.GetAllLoggedInAccounts() ?? new List<IDeveloperId>();
    }

    /// <summary>
    /// Gets all the repositories for an account and provider.  The account will be logged in if they aren't already.
    /// </summary>
    /// <param name="providerName">The specific provider.  Must match IRepositoryProvider.GetDisplayName</param>
    /// <param name="developerId">The account to look for.  May not be logged in.</param>
    /// <returns>All the repositories for an account and provider.</returns>
    public async Task<IEnumerable<IRepository>> GetAllRepositoriesAsync(string providerName, IDeveloperId developerId)
    {
        return await _providers.GetValueOrDefault(providerName)?.GetAllRepositoriesAsync(developerId) ?? new List<IRepository>();
    }
}
