// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Services;
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
    /// <summary>
    /// Hold all providers and organize by their names.
    /// </summary>
    private readonly Dictionary<string, RepositoryProvider> _providers = new ();

    public RepositoryProviders(IEnumerable<IPluginWrapper> pluginWrappers)
    {
        _providers = pluginWrappers.ToDictionary(pluginWrapper => pluginWrapper.Name, pluginWrapper => new RepositoryProvider(pluginWrapper));
    }

    /// <summary>
    /// Starts a provider if it isn't running.
    /// </summary>
    /// <param name="providerName">The provider to start.</param>
    public void StartIfNotRunning(string providerName)
    {
        if (_providers.ContainsKey(providerName))
        {
            _providers[providerName].StartIfNotRunning();
        }
    }

    /// <summary>
    /// Goes through all providers to figure out if they can make a repo from a Uri.
    /// </summary>
    /// <param name="uri">The Uri to parse.</param>
    /// <returns>If a provider was found that can parse the Uri then (providerName, repository) in not
    /// (string.empty, null)</returns>
    public (string, IRepository) ParseRepositoryFromUri(Uri uri)
    {
        foreach (var provider in _providers)
        {
            provider.Value.StartIfNotRunning();
            var repository = provider.Value.ParseRepositoryFromUri(uri).Result;
            if (repository != null)
            {
                return (provider.Key, repository);
            }
        }

        return (string.Empty, null);
    }

    /// <summary>
    /// Logs the user into a certain provider.
    /// </summary>
    /// <param name="providerName">The provider to log the user into.  Must match IRepositoryProvider.GetDisplayName</param>
    public IDeveloperId LogInToProvider(string providerName)
    {
        return _providers.GetValueOrDefault(providerName)?.LogIntoProvider();
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
