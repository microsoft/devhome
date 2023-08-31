// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.Telemetry;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Object that holds a reference to the providers in a plugin.
/// This needs to be changed to handle multiple accounts per provider.
/// </summary>
internal class RepositoryProvider
{
    /// <summary>
    /// All the repositories for an account.
    /// </summary>
    private readonly Lazy<IEnumerable<IRepository>> _repositories = new ();

    /// <summary>
    /// Wrapper for the plugin that is providing a repository and developer id.
    /// </summary>
    /// <remarks>
    /// The plugin is not started in the constructor.  It is started when StartIfNotRunningAsync is called.
    /// This is for lazy loading and starting and prevents all plugins from starting all at once.
    /// </remarks>
    private readonly IPluginWrapper _pluginWrapper;

    /// <summary>
    /// The DeveloperId provider used to log a user into an account.
    /// </summary>
    private IDeveloperIdProvider _devIdProvider;

    /// <summary>
    /// Provider used to clone a repsitory.
    /// </summary>
    private IRepositoryProvider _repositoryProvider;

    public RepositoryProvider(IPluginWrapper pluginWrapper)
    {
        _pluginWrapper = pluginWrapper;
    }

    public string DisplayName => _repositoryProvider.DisplayName;

    /// <summary>
    /// Starts the plugin if it isn't running.
    /// </summary>
    public void StartIfNotRunning()
    {
        // The task.run inside GetProvider makes a deadlock when .Result is called.
        // https://stackoverflow.com/a/17248813.  Solution is to wrap in Task.Run().
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Starting DevId and Repository provider plugins");
        _devIdProvider = Task.Run(() => _pluginWrapper.GetProviderAsync<IDeveloperIdProvider>()).Result;
        _repositoryProvider = Task.Run(() => _pluginWrapper.GetProviderAsync<IRepositoryProvider>()).Result;
        var myName = _repositoryProvider.DisplayName;
    }

    /// <summary>
    /// Tries to parse the repo name from the URI and makes a Repository from it.
    /// </summary>
    /// <param name="uri">The Uri to parse.</param>
    /// <returns>The repository the user wants to clone.  Null if parsing was unsuccessful.</returns>
    /// <remarks>
    /// Can be null if the provider can't parse the Uri.
    /// </remarks>
    public IRepository GetRepositoryFromUri(Uri uri, IDeveloperId developerId = null)
    {
        RepositoryResult getResult;
        if (developerId == null)
        {
            getResult = _repositoryProvider.GetRepositoryFromUriAsync(uri).AsTask().Result;
        }
        else
        {
            getResult = _repositoryProvider.GetRepositoryFromUriAsync(uri, developerId).AsTask().Result;
        }

        if (getResult.Result.Status == ProviderOperationStatus.Failure)
        {
            throw getResult.Result.ExtendedError;
        }

        return getResult.Repository;
    }

    /// <summary>
    /// Checks with the provider if it understands and can clone a repo via Uri.
    /// </summary>
    /// <param name="uri">The uri to the repository</param>
    /// <returns>A tuple that containes if the provider can parse the uri and the account it can parse with.</returns>
    /// <remarks>If the provider can't parse the Uri, this will try a second time with any logged in accounts.  If the repo is
    /// public, the developerid can be null.</remarks>
    public (bool, IDeveloperId, IRepositoryProvider) IsUriSupported(Uri uri)
    {
        return (false, null, null);
        /*
        var developerIdsResult = _devIdProvider.GetLoggedInDeveloperIds();

        // Possible that no accounts are loggd in.  Try in case the repo is public.
        if (developerIdsResult.Result.Status != ProviderOperationStatus.Success)
        {
            Log.Logger?.ReportError(Log.Component.RepoConfig, $"Could not get logged in accounts.  Message: {developerIdsResult.Result.DisplayMessage}", developerIdsResult.Result.ExtendedError);
            var uriSupportResult = Task.Run(() => _repositoryProvider.IsUriSupportedAsync(uri).AsTask()).Result;
            if (uriSupportResult.IsSupported)
            {
                return (true, null, _repositoryProvider);
            }
        }
        else
        {
            foreach (var developerId in developerIdsResult.DeveloperIds)
            {
                var uriSupportResult = Task.Run(() => _repositoryProvider.IsUriSupportedAsync(uri, developerId).AsTask()).Result;
                if (uriSupportResult.IsSupported)
                {
                    return (true, developerId, _repositoryProvider);
                }
            }
        }

        // no accounts can access this uri or the repo does not exist.
        return (false, null, null);
        */
    }

    /// <summary>
    /// Logs the current user into this provider
    /// </summary>
    public IDeveloperId LogIntoProvider()
    {
        /*
        return _devIdProvider.LoginNewDeveloperIdAsync().AsTask().Result;
        */

        /*
        return _devIdProvider.GetLoggedInDeveloperIds().DeveloperIds.First();
        */

        return null;
    }

    /// <summary>
    /// Gets all the logged in accounts for this provider.
    /// </summary>
    /// <returns>A list of all accounts.  May be empty.</returns>
    public IEnumerable<IDeveloperId> GetAllLoggedInAccounts()
    {
        return new List<IDeveloperId>();
        /*
        var developerIdsResult = _devIdProvider.GetLoggedInDeveloperIds();
        if (developerIdsResult.Result.Status != ProviderOperationStatus.Success)
        {
            Log.Logger?.ReportError(Log.Component.RepoConfig, $"Could not get logged in accounts.  Message: {developerIdsResult.Result.DisplayMessage}", developerIdsResult.Result.ExtendedError);
            return new List<IDeveloperId>();
        }

        return developerIdsResult.DeveloperIds;
        */
    }

    /// <summary>
    /// Gets all the repositories an account has for this provider.
    /// </summary>
    /// <param name="developerId">The account to search in.</param>
    /// <returns>A collection of repositories.  May be empty</returns>
    public IEnumerable<IRepository> GetAllRepositories(IDeveloperId developerId)
    {
        /*
        if (!_repositories.IsValueCreated)
        {
            TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetAllRepos_Event", LogLevel.Critical, new GetReposEvent("CallingExtension", _repositoryProvider.DisplayName, developerId));
            _repositories = new Lazy<IEnumerable<IRepository>>(_repositoryProvider.GetRepositoriesAsync(developerId).AsTask().Result);
        }

        TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetAllRepos_Event", LogLevel.Critical, new GetReposEvent("FoundRepos", _repositoryProvider.DisplayName, developerId));

        return _repositories.Value;
        */

        return new List<IRepository>();
    }
}
