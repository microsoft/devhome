// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Renderers;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.Common.Views;
using DevHome.Logging;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.Telemetry;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;
using Windows.Storage;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Object that holds a reference to the providers in a extension.
/// This needs to be changed to handle multiple accounts per provider.
/// </summary>
internal sealed class RepositoryProvider
{
    /// <summary>
    /// Wrapper for the extension that is providing a repository and developer id.
    /// </summary>
    /// <remarks>
    /// The extension is not started in the constructor.  It is started when StartIfNotRunningAsync is called.
    /// This is for lazy loading and starting and prevents all extensions from starting all at once.
    /// </remarks>
    private readonly IExtensionWrapper _extensionWrapper;

    /// <summary>
    /// Dictionary with all the repositories per account.
    /// </summary>
    private Dictionary<IDeveloperId, IEnumerable<IRepository>> _repositories = new ();

    /// <summary>
    /// The DeveloperId provider used to log a user into an account.
    /// </summary>
    private IDeveloperIdProvider _devIdProvider;

    /// <summary>
    /// Provider used to clone a repsitory.
    /// </summary>
    private IRepositoryProvider _repositoryProvider;

    /// <summary>
    /// Used to enable searching for repos with a query.
    /// </summary>
    private IRepositoryProvider2 _repositoryProvider2;

    public RepositoryProvider(IExtensionWrapper extensionWrapper)
    {
        _extensionWrapper = extensionWrapper;
    }

    public string DisplayName => _repositoryProvider.DisplayName;

    public string ExtensionDisplayName => _extensionWrapper.Name;

    /// <summary>
    /// Starts the extension if it isn't running.
    /// </summary>
    public void StartIfNotRunning()
    {
        // The task.run inside GetProvider makes a deadlock when .Result is called.
        // https://stackoverflow.com/a/17248813.  Solution is to wrap in Task.Run().
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Starting DevId and Repository provider extensions");
        _devIdProvider = Task.Run(() => _extensionWrapper.GetProviderAsync<IDeveloperIdProvider>()).Result;
        _repositoryProvider = Task.Run(() => _extensionWrapper.GetProviderAsync<IRepositoryProvider>()).Result;

        // _repositoryProvider2 == null is used to modify behavior.
        _repositoryProvider2 = _repositoryProvider as IRepositoryProvider2;
    }

    public IRepositoryProvider GetProvider()
    {
        return _repositoryProvider;
    }

    /// <summary>
    /// Asks the provider for search terms for querying repositories.
    /// </summary>
    /// <returns>The names of the search fields.</returns>
    public List<string> GetSearchTerms()
    {
        return _repositoryProvider2?.GetSearchFieldNames.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Asks the provider for a list of suggestions, given values of other search terms.
    /// </summary>
    /// <param name="developerId">The logged in user.</param>
    /// <param name="searchTerms">All information found in the search grid</param>
    /// <param name="fieldName">The field to request data for</param>
    /// <returns>A list of names that can be used for the field.  An empty list is returned if the provider isn't found</returns>
    public List<string> GetValuesFor(IDeveloperId developerId, Dictionary<string, string> searchTerms, string fieldName)
    {
        return _repositoryProvider2?.GetValuesForField(fieldName, searchTerms, developerId).AsTask().Result.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Asks the provider, given the fieldName, what was the value of the most recent search.
    /// </summary>
    /// <param name="fieldName">The search field to ask for</param>
    /// <returns>A string representing the term used in the most recent search.  If the provider can't be found
    /// string.empty is returned.</returns>
    public string GetFieldSearchValue(IDeveloperId developerId, string fieldName)
    {
        return _repositoryProvider2?.GetFieldSearchValue(fieldName, developerId) ?? string.Empty;
    }

    /// <summary>
    /// Assigns handler as the event handler for the developerIdProvider.
    /// </summary>
    /// <param name="handler">The method to run.</param>
    public void SetChangedEvent(TypedEventHandler<IDeveloperIdProvider, IDeveloperId> handler)
    {
        if (_devIdProvider != null)
        {
            _devIdProvider.Changed += handler;
        }
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
            Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Could not get repo from Uri.");
            Log.Logger?.ReportInfo(Log.Component.RepoConfig, getResult.Result.DisplayMessage);
            return null;
        }

        return getResult.Repository;
    }

    /// <summary>
    /// Checks with the provider if it understands and can clone a repo via Uri.
    /// </summary>
    /// <param name="uri">The uri to the repository</param>
    /// <returns>True if this provider supports the url.  False otherwise.</returns>
    public bool IsUriSupported(Uri uri)
    {
        var uriSupportResult = Task.Run(() => _repositoryProvider.IsUriSupportedAsync(uri).AsTask()).Result;
        if (uriSupportResult.Result.Status == ProviderOperationStatus.Failure)
        {
            return false;
        }

        return uriSupportResult.IsSupported;
    }

    /// <summary>
    /// Gets and configures the UI to show to the user for logging them in.
    /// </summary>
    /// <param name="elementTheme">The theme to use.</param>
    /// <returns>The adaptive panel to show to the user.  Can be null.</returns>
    public ExtensionAdaptiveCardPanel GetLoginUi(ElementTheme elementTheme)
    {
        try
        {
            var adaptiveCardSessionResult = _devIdProvider.GetLoginAdaptiveCardSession();
            if (adaptiveCardSessionResult.Result.Status == ProviderOperationStatus.Failure)
            {
                GlobalLog.Logger?.ReportError($"{adaptiveCardSessionResult.Result.DisplayMessage} - {adaptiveCardSessionResult.Result.DiagnosticText}");
                return null;
            }

            var loginUIAdaptiveCardController = adaptiveCardSessionResult.AdaptiveCardSession;
            var renderer = new AdaptiveCardRenderer();
            ConfigureLoginUIRenderer(renderer, elementTheme).Wait();
            renderer.HostConfig.ContainerStyles.Default.BackgroundColor = Microsoft.UI.Colors.Transparent;

            var extensionAdaptiveCardPanel = new ExtensionAdaptiveCardPanel();
            extensionAdaptiveCardPanel.Bind(loginUIAdaptiveCardController, renderer);
            extensionAdaptiveCardPanel.RequestedTheme = elementTheme;

            return extensionAdaptiveCardPanel;
        }
        catch (Exception ex)
        {
            GlobalLog.Logger?.ReportError($"ShowLoginUIAsync(): loginUIContentDialog failed.", ex);
        }

        return null;
    }

    /// <summary>
    /// Sets the renderer in the UI.
    /// </summary>
    /// <param name="renderer">The ui to show</param>
    /// <param name="elementTheme">The theme to use</param>
    /// <returns>A task to await on.</returns>
    private async Task ConfigureLoginUIRenderer(AdaptiveCardRenderer renderer, ElementTheme elementTheme)
    {
        var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        // Add custom Adaptive Card renderer for LoginUI as done for Widgets.
        renderer.ElementRenderers.Set(LabelGroup.CustomTypeString, new LabelGroupRenderer());

        var hostConfigContents = string.Empty;
        var hostConfigFileName = (elementTheme == ElementTheme.Light) ? "LightHostConfig.json" : "DarkHostConfig.json";
        try
        {
            var uri = new Uri($"ms-appx:////DevHome.Settings/Assets/{hostConfigFileName}");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().ConfigureAwait(false);
            hostConfigContents = await FileIO.ReadTextAsync(file);
        }
        catch (Exception ex)
        {
            GlobalLog.Logger?.ReportError($"Failure occurred while retrieving the HostConfig file - HostConfigFileName: {hostConfigFileName}.", ex);
        }

        // Add host config for current theme to renderer
        dispatcher.TryEnqueue(() =>
        {
            if (!string.IsNullOrEmpty(hostConfigContents))
            {
                renderer.HostConfig = AdaptiveHostConfig.FromJsonString(hostConfigContents).HostConfig;
            }
            else
            {
                GlobalLog.Logger?.ReportInfo($"HostConfig file contents are null or empty - HostConfigFileContents: {hostConfigContents}");
            }
        });
        return;
    }

    public AuthenticationExperienceKind GetAuthenticationExperienceKind()
    {
        return _devIdProvider.GetAuthenticationExperienceKind();
    }

    public IAsyncOperation<DeveloperIdResult> ShowLogonBehavior(WindowId windowHandle)
    {
        return _devIdProvider.ShowLogonSession(windowHandle);
    }

    /// <summary>
    /// Gets all the logged in accounts for this provider.
    /// </summary>
    /// <returns>A list of all accounts.  May be empty.</returns>
    public IEnumerable<IDeveloperId> GetAllLoggedInAccounts()
    {
        var developerIdsResult = _devIdProvider.GetLoggedInDeveloperIds();
        if (developerIdsResult.Result.Status != ProviderOperationStatus.Success)
        {
            Log.Logger?.ReportError(Log.Component.RepoConfig, $"Could not get logged in accounts.  Message: {developerIdsResult.Result.DisplayMessage}", developerIdsResult.Result.ExtendedError);
            return new List<IDeveloperId>();
        }

        return developerIdsResult.DeveloperIds;
    }

    /// <summary>
    /// Gets all the repositories an account has for this provider.
    /// </summary>
    /// <param name="developerId">The account to search in.</param>
    /// <returns>A collection of repositories.  May be empty</returns>
    public IEnumerable<IRepository> GetAllRepositories(IDeveloperId developerId, Dictionary<string, string> searchInputs)
    {
        TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetAllRepos_Event", LogLevel.Critical, new GetReposEvent("CallingExtension", _repositoryProvider.DisplayName, developerId));

        RepositoriesResult result;
        try
        {
            if (IsSearchingEnabled())
            {
                result = _repositoryProvider2.GetRepositoriesAsync(searchInputs, developerId).AsTask().Result;
            }
            else
            {
                result = _repositoryProvider.GetRepositoriesAsync(developerId).AsTask().Result;
            }
        }
        catch (AggregateException aggregateException)
        {
            // Because tasks can be cancled DevHome should emit different logs.
            if (aggregateException.InnerException is OperationCanceledException)
            {
                GlobalLog.Logger?.ReportInfo($"Get Repos operation was cancalled.");
            }
            else
            {
                GlobalLog.Logger?.ReportInfo($"{aggregateException.ToString()}");
            }

            _repositories[developerId] = new List<IRepository>();
            return _repositories[developerId];
        }

        if (result.Result.Status != ProviderOperationStatus.Success)
        {
            if (_repositories.TryGetValue(developerId, out var _))
            {
                _repositories[developerId] = new List<IRepository>();
            }
            else
            {
                _repositories.Add(developerId, new List<IRepository>());
            }
        }
        else
        {
            if (_repositories.TryGetValue(developerId, out var _))
            {
                _repositories[developerId] = result.Repositories;
            }
            else
            {
                _repositories.Add(developerId, result.Repositories);
            }
        }

        TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetAllRepos_Event", LogLevel.Critical, new GetReposEvent("FoundRepos", _repositoryProvider.DisplayName, developerId));

        return _repositories[developerId];
    }

    /// <summary>
    /// Gets all the repositories an account has for this provider.
    /// </summary>
    /// <param name="developerId">The account to search in.</param>
    /// <returns>A collection of repositories.  May be empty</returns>
    public IEnumerable<IRepository> GetAllRepositories(IDeveloperId developerId)
    {
        return GetAllRepositories(developerId, new ());
    }

    public bool IsSearchingEnabled()
    {
        return _repositoryProvider2 != null;
    }
}
