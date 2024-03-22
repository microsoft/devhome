// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    private readonly Dictionary<IDeveloperId, IEnumerable<IRepository>> _repositories = new();

    /// <summary>
    /// The DeveloperId provider used to log a user into an account.
    /// </summary>
    private IDeveloperIdProvider _devIdProvider;

    /// <summary>
    /// Provider used to clone a repsitory.
    /// </summary>
    private IRepositoryProvider _repositoryProvider;

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
        try
        {
            _devIdProvider = Task.Run(() => _extensionWrapper.GetProviderAsync<IDeveloperIdProvider>()).Result;
            _repositoryProvider = Task.Run(() => _extensionWrapper.GetProviderAsync<IRepositoryProvider>()).Result;
        }
        catch (Exception ex)
        {
            Log.Logger?.ReportError(Log.Component.RepoConfig, $"Could not get repository provider from extension.", ex);
        }
    }

    public IRepositoryProvider GetProvider()
    {
        return _repositoryProvider;
    }

    public string GetAskChangeSearchFieldsLabel()
    {
        var repositoryProvider2 = _repositoryProvider as IRepositoryProvider2;
        return repositoryProvider2?.AskToSearchLabel ?? string.Empty;
    }

    /// <summary>
    /// Asks the provider for search terms for querying repositories.
    /// </summary>
    /// <returns>The names of the search fields.</returns>
    public List<string> GetSearchTerms()
    {
        var repositoryProvider2 = _repositoryProvider as IRepositoryProvider2;
        return repositoryProvider2?.SearchFieldNames.ToList() ?? new List<string>();
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
        var repositoryProvider2 = _repositoryProvider as IRepositoryProvider2;
        return repositoryProvider2?.GetValuesForSearchFieldAsync(searchTerms, fieldName, developerId).AsTask().Result.ToList() ?? new List<string>();
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
        renderer.ElementRenderers.Set("Input.ChoiceSet", new AccessibleChoiceSet());

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

                // Remove margins from selectAction.
                renderer.AddSelectActionMargin = false;
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

    public RepositorySearchInformation SearchForRepositories(IDeveloperId developerId, Dictionary<string, string> searchInputs)
    {
        TelemetryFactory.Get<ITelemetry>().Log("RepoTool_SearchForRepos_Event", LogLevel.Critical, new GetReposEvent("CallingExtension", _repositoryProvider.DisplayName, developerId));

        var repoSearchInformation = new RepositorySearchInformation();
        try
        {
            if (_repositoryProvider is IRepositoryProvider2 repositoryProvider2 &&
                IsSearchingEnabled() && searchInputs != null)
            {
                var result = repositoryProvider2.GetRepositoriesAsync(searchInputs, developerId).AsTask().Result;
                if (result.Result.Status == ProviderOperationStatus.Success)
                {
                    repoSearchInformation.Repositories = result.Repositories;
                    repoSearchInformation.SelectionOptionsPlaceHolderText = result.SelectionOptionsName;
                    repoSearchInformation.SelectionOptionsLabel = result.SelectionOptionsLabel;
                    repoSearchInformation.SelectionOptions = result.SelectionOptions.ToList();
                }
                else
                {
                    Log.Logger?.ReportError(Log.Component.RepoConfig, $"Could not get repositories.  Message: {result.Result.DisplayMessage}", result.Result.ExtendedError);
                }
            }
            else
            {
                // Fallback in case this is called with IRepositoryProvider.
                RepositoriesResult result = _repositoryProvider.GetRepositoriesAsync(developerId).AsTask().Result;
                if (result.Result.Status == ProviderOperationStatus.Success)
                {
                    repoSearchInformation.Repositories = result.Repositories;
                }
                else
                {
                    Log.Logger?.ReportError(Log.Component.RepoConfig, $"Could not get repositories.  Message: {result.Result.DisplayMessage}", result.Result.ExtendedError);
                }
            }
        }
        catch (AggregateException aggregateException)
        {
            // Because tasks can be canceled DevHome should emit different logs.
            if (aggregateException.InnerException is OperationCanceledException)
            {
                Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Get Repos operation was cancalled.");
            }
            else
            {
                Log.Logger?.ReportInfo(Log.Component.RepoConfig, aggregateException.ToString());
            }
        }
        catch (Exception ex)
        {
            Log.Logger?.ReportError(Log.Component.RepoConfig, $"Could not get repositories.  Message: {ex}");
        }

        _repositories[developerId] = repoSearchInformation.Repositories;

        TelemetryFactory.Get<ITelemetry>().Log("RepoTool_SearchForRepos_Event", LogLevel.Critical, new GetReposEvent("FoundRepos", _repositoryProvider.DisplayName, developerId));
        return repoSearchInformation;
    }

    public RepositorySearchInformation GetAllRepositories(IDeveloperId developerId)
    {
        TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetAllRepos_Event", LogLevel.Critical, new GetReposEvent("CallingExtension", _repositoryProvider.DisplayName, developerId));
        var repoSearchInformation = new RepositorySearchInformation();
        try
        {
            var result = _repositoryProvider.GetRepositoriesAsync(developerId).AsTask().Result;
            if (result.Result.Status == ProviderOperationStatus.Success)
            {
                repoSearchInformation.Repositories = result.Repositories;
            }
            else
            {
                Log.Logger?.ReportError(Log.Component.RepoConfig, $"Could not get repositories.  Message: {result.Result.DisplayMessage}", result.Result.ExtendedError);
            }
        }
        catch (AggregateException aggregateException)
        {
            // Because tasks can be canceled DevHome should emit different logs.
            if (aggregateException.InnerException is OperationCanceledException)
            {
                GlobalLog.Logger?.ReportInfo($"Get Repos operation was cancalled.");
            }
            else
            {
                GlobalLog.Logger?.ReportError($"{aggregateException}");
            }
        }
        catch (Exception ex)
        {
            Log.Logger?.ReportError(Log.Component.RepoConfig, $"Could not get repositories.  Message: {ex}");
        }

        _repositories[developerId] = repoSearchInformation.Repositories;

        TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetAllRepos_Event", LogLevel.Critical, new GetReposEvent("FoundRepos", _repositoryProvider.DisplayName, developerId));
        return repoSearchInformation;
    }

    /// <summary>
    /// Checks if
    /// 1. _repositoryProvider is IRepositoryProvider2,
    /// 2. if it is, calls IsSearchingSupported.
    /// </summary>
    /// <returns>If the extension implements IRepositoryProvider2.</returns>
    public bool IsSearchingEnabled()
    {
        if (_repositoryProvider is IRepositoryProvider2 repoProviderWithSearch)
        {
            return repoProviderWithSearch.IsSearchingSupported;
        }

        return false;
    }
}
