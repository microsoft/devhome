// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using DevHome.Common.Services;
using DevHome.Common.Views;
using DevHome.SetupFlow.Common.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models;

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

    public string DisplayName(string providerName)
    {
        return _providers.GetValueOrDefault(providerName)?.DisplayName ?? string.Empty;
    }

    public RepositoryProviders(IEnumerable<IPluginWrapper> pluginWrappers)
    {
        _providers = pluginWrappers.ToDictionary(pluginWrapper => pluginWrapper.Name, pluginWrapper => new RepositoryProvider(pluginWrapper));
    }

    public void StartAllPlugins()
    {
        foreach (var pluginWrapper in _providers.Values)
        {
            pluginWrapper.StartIfNotRunning();
        }
    }

    /// <summary>
    /// Starts a provider if it isn't running.
    /// </summary>
    /// <param name="providerName">The provider to start.</param>
    public void StartIfNotRunning(string providerName)
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Starting RepositoryProvider {providerName}");
        if (_providers.ContainsKey(providerName))
        {
            _providers[providerName].StartIfNotRunning();
        }
    }

    /// <summary>
    /// Goes through all providers to figure out if they can make a repo from a Uri.
    /// </summary>
    /// <param name="uri">The Uri to parse.</param>
    /// <returns>If a provider was found that can parse the Uri then (providerName, repository) if not
    /// (string.empty, null)</returns>
    public (string, IRepository) GetRepositoryFromUri(Uri uri)
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Parsing repository from URI {uri}");
        foreach (var provider in _providers)
        {
            provider.Value.StartIfNotRunning();
            Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Attempting to parse using provider {provider.Key}");
            var repository = provider.Value.GetRepositoryFromUri(uri);
            if (repository != null)
            {
                Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Repository parsed to {repository.DisplayName} owned by {repository.OwningAccountName}");
                return (provider.Value.DisplayName, repository);
            }
        }

        return (string.Empty, null);
    }

    /// <summary>
    /// Queries each provider to figure out if it can support the URI and can clone from it.
    /// </summary>
    /// <param name="uri">The uri that points to a remote repository</param>
    /// <returns>THe provider that can clone the repo.  Otherwise null.</returns>
    public RepositoryProvider CanAnyProviderSupportThisUri(Uri uri)
    {
        foreach (var provider in _providers)
        {
            provider.Value.StartIfNotRunning();
            var isSupported = provider.Value.IsUriSupported(uri);
            if (isSupported)
            {
                return provider.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Queries each provider to figure out if it can support the URI and can clone from it.
    /// </summary>
    /// <param name="uri">The uri that points to a remote repository</param>
    /// <returns>THe provider that can clone the repo.  Otherwise null.</returns>
    public (bool, IDeveloperId, IRepositoryProvider) CanAnyProviderSupportThisUri(Uri uri)
    {
        foreach (var provider in _providers)
        {
            provider.Value.StartIfNotRunning();
            var isSupported = provider.Value.IsUriSupported(uri);
            if (isSupported.Item1)
            {
                return isSupported;
            }
        }

        return (false, null, null);
    }

    public PluginAdaptiveCardPanel GetLoginUi(string providerName, ElementTheme elementTheme)
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Getting login UI {providerName}");
        return _providers.GetValueOrDefault(providerName)?.GetLoginUi(elementTheme);
    }

    /// <summary>
    /// Gets the display names of all providers.
    /// </summary>
    /// <returns>A collection of display names.</returns>
    public IEnumerable<string> GetAllProviderNames()
    {
        return _providers.Keys;
    }

    public IRepositoryProvider GetProvider(string providerName)
    {
        if (_providers.TryGetValue(providerName, out var repoProvider))
        {
            return repoProvider.GetProvider();
        }

        return null;
    }

    /// <summary>
    /// Gets all logged in accounts for a specific provider.
    /// </summary>
    /// <param name="providerName">The provider to use.  Must match the display name of the provider</param>
    /// <returns>A collection of developer Ids of all logged in users.  Can be empty.</returns>
    public IEnumerable<IDeveloperId> GetAllLoggedInAccounts(string providerName)
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Getting all logged in accounts for repository provider {providerName}");
        return _providers.GetValueOrDefault(providerName)?.GetAllLoggedInAccounts() ?? new List<IDeveloperId>();
    }

    /// <summary>
    /// Gets all the repositories for an account and provider.  The account will be logged in if they aren't already.
    /// </summary>
    /// <param name="providerName">The specific provider.  Must match the display name of a provider</param>
    /// <param name="developerId">The account to look for.  May not be logged in.</param>
    /// <returns>All the repositories for an account and provider.</returns>
    public IEnumerable<IRepository> GetAllRepositories(string providerName, IDeveloperId developerId)
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Getting all repositories for repository provider {providerName}");
        return _providers.GetValueOrDefault(providerName)?.GetAllRepositories(developerId) ?? new List<IRepository>();
    }
}
