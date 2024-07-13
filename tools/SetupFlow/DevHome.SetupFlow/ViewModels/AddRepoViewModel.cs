// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.DeveloperId;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.Contracts.Services;
using DevHome.Logging;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;
using WinUIEx;
using static DevHome.SetupFlow.Models.Common;

namespace DevHome.SetupFlow.ViewModels;

/// <summary>
/// View model to handle the top layer of the repo tool including
/// 1. Repo Review
/// 2. Switching between account, repositories, and url page
/// </summary>
public partial class AddRepoViewModel : ObservableObject
{
    private readonly IHost _host;

    private readonly Guid _activityId;

    private readonly ISetupFlowStringResource _stringResource;

    private readonly List<CloningInformation> _previouslySelectedRepos;

    private ElementTheme SelectedTheme => _host.GetService<IThemeSelectorService>().Theme;

    /// <summary>
    /// Gets or sets a value indicating whether the log-in prompt is on screen.
    /// </summary>
    public bool IsLoggingIn
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the list that keeps all repositories the user wants to clone.
    /// </summary>
    public List<CloningInformation> EverythingToClone
    {
        get; set;
    }

    /// <summary>
    /// The url of the repository the user wants to clone.
    /// </summary>
    [ObservableProperty]
    private string _url = string.Empty;

    /// <summary>
    /// All the providers Dev Home found.  Used for logging in the accounts and getting all repositories.
    /// </summary>
    private RepositoryProviders _providers;

    /// <summary>
    /// The list of all repositories shown to the user on the repositories page.
    /// </summary>
    private IEnumerable<IRepository> _repositoriesForAccount;

    /// <summary>
    /// Names of all providers.  This is shown to the user on the accounts page.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _providerNames = new ();

    /// <summary>
    /// Names of all accounts the user has logged into for a particular provider.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAccountComboBoxEnabled))]
    private ObservableCollection<string> _accounts = new ();

    /// <summary>
    /// The currently selected account.
    /// </summary>
    private string _selectedAccount;

    /// <summary>
    /// All repositories currently shown on the screen.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<RepoViewListItem> _repositories = new ();

    /// <summary>
    /// Should the URL page be visible?
    /// </summary>
    [ObservableProperty]
    private Visibility _showUrlPage;

    /// <summary>
    /// Should the account page be visible?
    /// </summary>
    [ObservableProperty]
    private Visibility _showAccountPage;

    /// <summary>
    /// Should the repositories page be visible?
    /// </summary>
    [ObservableProperty]
    private Visibility _showRepoPage;

    /// <summary>
    /// Should the error text be shown?
    /// </summary>
    [ObservableProperty]
    private Visibility _showErrorTextBox;

    /// <summary>
    /// Keeps track of if the account button is checked.  Used to switch UIs
    /// </summary>
    [ObservableProperty]
    private bool? _isAccountToggleButtonChecked;

    [ObservableProperty]
    private bool _isAccountButtonEnabled;

    /// <summary>
    /// Keeps track if the URL button is checked.  Used to switch UIs
    /// </summary>
    [ObservableProperty]
    private bool? _isUrlAccountButtonChecked;

    [ObservableProperty]
    private string _primaryButtonText;

    [ObservableProperty]
    private string _urlParsingError;

    public bool IsAccountComboBoxEnabled => Accounts.Count > 1;

    [ObservableProperty]
    private Visibility _shouldShowUrlError;

    [ObservableProperty]
    private bool _isFetchingRepos;

    [ObservableProperty]
    private bool _shouldEnablePrimaryButton;

    [ObservableProperty]
    private Style _styleForPrimaryButton;

    [ObservableProperty]
    private bool _shouldShowLoginUi;

    [ObservableProperty]
    private bool _shouldShowXButtonInLoginUi;

    [ObservableProperty]
    private bool _isCancelling;

    /// <summary>
    /// Indicates if the ListView is currently filtering items.  A result of manually filtering a list view
    /// is that the SelectionChanged is fired for any selected item that is removed and the item isn't "re-selected"
    /// To prevent our EverythingToClone from changing this flag is used.
    /// If true any removals caused by filtering are ignored.
    /// Question.  If the items aren't "re-selected" how do they become selected?  The list view has SelectRange
    /// that can be used to re-select items.  This is done in the view.
    /// </summary>
    private bool _isFiltering;

    /// <summary>
    /// Gets or sets a value indicating whether the SelectionChange event fired because SelectRange was called.
    /// After filtering SelectRange is called to re-select all previously selected items.  This causes SelectionChanged
    /// to be fired for each item.  Because EverythingToClone didn't change during filtering it contains every item to select.
    /// This flag is to prevent adding duplicate items are being re-selected.
    /// </summary>
    public bool IsCallingSelectRange { get; set; }

    /// <summary>
    /// Filters all repos down to any that start with text.
    /// A side-effect of filtering is that SelectionChanged fires for every selected repo but only on removal.
    /// SelectionChanged isn't fired for re-adding because repos are removed, not added.  To prevent the RepoTool from forgetting the repos that were selected
    /// the flag _isFiltering is used to prevent modifications to EverythingToClone.
    /// Once filtering is done SelectRange is called on each item in EverythingToClone to re-select them.
    /// </summary>
    /// <param name="text">The text to use with .StartsWith</param>
    public void FilterRepositories(string text)
    {
        IEnumerable<IRepository> filteredRepositories;
        if (text.Equals(string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            filteredRepositories = _repositoriesForAccount;
        }
        else
        {
            filteredRepositories = _repositoriesForAccount
                ?.Where(x => x.DisplayName.StartsWith(text, StringComparison.OrdinalIgnoreCase));
        }

        _isFiltering = true;
        Repositories = new ObservableCollection<RepoViewListItem>(OrderRepos(filteredRepositories ?? ImmutableArray<IRepository>.Empty));
        _isFiltering = false;
    }

    /// <summary>
    /// Order repos in a particular order.  The order is
    /// 1. User Private repos
    /// 2. Org repos
    /// 3. User Public repos.
    /// Each section is ordered by the most recently updated.
    /// </summary>
    /// <param name="repos">The list of repos to order.</param>
    /// <returns>An enumerable collection of items ready to be put into the ListView</returns>
    private IEnumerable<RepoViewListItem> OrderRepos(IEnumerable<IRepository> repos)
    {
        var organizationRepos = repos.Where(x => !x.OwningAccountName.Equals(_selectedAccount, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.LastUpdated)
            .Select(x => new RepoViewListItem(x));

        var userRepos = repos.Where(x => x.OwningAccountName.Equals(_selectedAccount, StringComparison.OrdinalIgnoreCase));
        var userPublicRepos = userRepos.Where(x => !x.IsPrivate)
            .OrderByDescending(x => x.LastUpdated)
            .Select(x => new RepoViewListItem(x));

        var userPrivateRepos = userRepos.Where(x => x.IsPrivate)
            .OrderByDescending(x => x.LastUpdated)
            .Select(x => new RepoViewListItem(x));

        return userPrivateRepos
            .Concat(organizationRepos)
            .Concat(userPublicRepos);
    }

    /// <summary>
    /// Gets a value indicating whether the UI can skip the account page and switch to the repo page.
    /// </summary>
    /// <remarks>
    /// UI can skip the account tab and go to the repo page if the following conditions are met
    /// 1. DevHome has only 1 provider installed.
    /// 2. The provider has only 1 logged in account.
    /// </remarks>
    public bool CanSkipAccountConnection
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets or sets what page the user is currently on.  Used to branch logic depending on the page.
    /// </summary>
    internal PageKind CurrentPage
    {
        get; set;
    }

    private TypedEventHandler<IDeveloperIdProvider, IDeveloperId> _developerIdChangedEvent;

    /// <summary>
    /// Logs the user into the provider if they aren't already.
    /// Changes the page to show all repositories for the user.
    /// </summary>
    /// <remarks>
    /// Fired when the combo box on the account page is changed.
    /// </remarks>
    [RelayCommand]
    private void RepoProviderSelected(string repositoryProviderName)
    {
        if (!string.IsNullOrEmpty(repositoryProviderName))
        {
            StyleForPrimaryButton = Application.Current.Resources["SystemAccentColor"] as Style;
            ShouldEnablePrimaryButton = true;
        }
        else
        {
            StyleForPrimaryButton = Application.Current.Resources["DefaultButtonStyle"] as Style;
            ShouldEnablePrimaryButton = false;
        }
    }

    [RelayCommand]
    private void CancelButtonPressed()
    {
        IsLoggingIn = false;
        IsCancelling = true;
    }

    public AddRepoViewModel(
        ISetupFlowStringResource stringResource,
        List<CloningInformation> previouslySelectedRepos,
        IHost host,
        Guid activityId)
    {
        _stringResource = stringResource;
        _host = host;
        ChangeToUrlPage();

        // override changes ChangeToUrlPage to correctly set the state.
        UrlParsingError = string.Empty;
        ShouldShowUrlError = Visibility.Collapsed;
        ShowErrorTextBox = Visibility.Collapsed;

        _previouslySelectedRepos = previouslySelectedRepos ?? new List<CloningInformation>();
        EverythingToClone = new List<CloningInformation>(_previouslySelectedRepos);
        _activityId = activityId;
    }

    /// <summary>
    /// Gets all the extensions the DevHome can see.
    /// </summary>
    /// <remarks>
    /// A valid extension is one that has a repository provider and developerId provider.
    /// </remarks>
    public void GetExtensions()
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Getting installed extensions with Repository and DevId providers");
        var extensionService = Application.Current.GetService<IExtensionService>();
        var extensionWrappers = extensionService.GetInstalledExtensionsAsync().Result;

        var extensions = extensionWrappers.Where(
            extension => extension.HasProviderType(ProviderType.Repository) &&
            extension.HasProviderType(ProviderType.DeveloperId));

        _providers = new RepositoryProviders(extensions);

        // Start all extensions to get the DisplayName of each provider.
        _providers.StartAllExtensions();

        ProviderNames = new ObservableCollection<string>(_providers.GetAllProviderNames());
        TelemetryFactory.Get<ITelemetry>().Log("RepoTool_SearchForExtensions_Event", LogLevel.Critical, new ExtensionEvent(ProviderNames.Count), _activityId);

        IsAccountButtonEnabled = extensions.Any();
    }

    public void SetChangedEvents(TypedEventHandler<IDeveloperIdProvider, IDeveloperId> handler)
    {
        _developerIdChangedEvent = handler;
    }

    public void ChangeToUrlPage()
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Changing to Url page");
        ShowUrlPage = Visibility.Visible;
        ShowAccountPage = Visibility.Collapsed;
        ShowRepoPage = Visibility.Collapsed;
        IsUrlAccountButtonChecked = true;
        IsAccountToggleButtonChecked = false;
        CurrentPage = PageKind.AddViaUrl;
        PrimaryButtonText = _stringResource.GetLocalized(StringResourceKey.RepoEverythingElsePrimaryButtonText);
        ShouldShowLoginUi = false;
    }

    public void ChangeToAccountPage()
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Changing to Account page");
        ShouldShowUrlError = Visibility.Collapsed;
        ShowUrlPage = Visibility.Collapsed;
        ShowAccountPage = Visibility.Visible;
        ShowRepoPage = Visibility.Collapsed;
        IsUrlAccountButtonChecked = false;
        IsAccountToggleButtonChecked = true;
        CurrentPage = PageKind.AddViaAccount;
        PrimaryButtonText = _stringResource.GetLocalized(StringResourceKey.RepoAccountPagePrimaryButtonText);
        ShouldShowLoginUi = false;

        // List of extensions needs to be refreshed before accessing
        GetExtensions();
        if (ProviderNames.Count == 1)
        {
            _providers.StartIfNotRunning(ProviderNames[0]);
            var accounts = _providers.GetAllLoggedInAccounts(ProviderNames[0]);
            if (accounts.Count() == 1)
            {
                CanSkipAccountConnection = true;
            }
        }
    }

    public void ChangeToRepoPage()
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Changing to Repo page");
        ShowUrlPage = Visibility.Collapsed;
        ShowAccountPage = Visibility.Collapsed;
        ShowRepoPage = Visibility.Visible;
        CurrentPage = PageKind.Repositories;
        PrimaryButtonText = _stringResource.GetLocalized(StringResourceKey.RepoEverythingElsePrimaryButtonText);
        ShouldShowLoginUi = false;

        // The only way to get the repo page is through the account page.
        // No need to change toggle buttons.
    }

    /// <summary>
    /// Makes sure all needed information is present.
    /// </summary>
    /// <returns>True if all information is in order, otherwise false</returns>
    public bool ValidateRepoInformation()
    {
        if (CurrentPage == PageKind.AddViaUrl)
        {
            // Check if Url field is empty
            if (string.IsNullOrEmpty(Url))
            {
                return false;
            }

            if (!Uri.TryCreate(Url, UriKind.RelativeOrAbsolute, out _))
            {
                UrlParsingError = _stringResource.GetLocalized(StringResourceKey.UrlValidationBadUrl);
                ShouldShowUrlError = Visibility.Visible;
                return false;
            }

            var sshMatch = Regex.Match(Url, "^.*@.*:.*\\/.*");

            if (sshMatch.Success)
            {
                UrlParsingError = _stringResource.GetLocalized(StringResourceKey.SSHConnectionStringNotAllowed);
                ShouldShowUrlError = Visibility.Visible;
                return false;
            }

            ShouldShowUrlError = Visibility.Collapsed;
            return true;
        }
        else if (CurrentPage == PageKind.AddViaAccount || CurrentPage == PageKind.Repositories)
        {
             return EverythingToClone.Count > 0;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Gets all the accounts for a provider and updates the UI.
    /// </summary>
    /// <param name="repositoryProviderName">The provider the user wants to use.</param>
    public async Task GetAccountsAsync(string repositoryProviderName, Frame loginFrame)
    {
        await Task.Run(() => _providers.StartIfNotRunning(repositoryProviderName));
        var loggedInAccounts = await Task.Run(() => _providers.GetAllLoggedInAccounts(repositoryProviderName));
        if (!loggedInAccounts.Any())
        {
            IsLoggingIn = true;
            ShouldShowLoginUi = true;

            // AddRepoDialog can handle the close button click.  Don't show the x button.
            ShouldShowXButtonInLoginUi = false;
            InitiateAddAccountUserExperienceAsync(_providers.GetProvider(repositoryProviderName), loginFrame);

            // Wait 30 seconds for user to log in.
            var maxIterationsToWait = 30;
            var currentIteration = 0;
            var waitDelay = Convert.ToInt32(new TimeSpan(0, 0, 1).TotalMilliseconds);
            while ((IsLoggingIn && !IsCancelling) && currentIteration++ <= maxIterationsToWait)
            {
                await Task.Delay(waitDelay);
            }

            ShouldShowLoginUi = false;
            loggedInAccounts = await Task.Run(() => _providers.GetAllLoggedInAccounts(repositoryProviderName));
            TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetAccount_Event", LogLevel.Critical, new RepoDialogGetAccountEvent(repositoryProviderName, alreadyLoggedIn: false), _activityId);
        }
        else
        {
            TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetAccount_Event", LogLevel.Critical, new RepoDialogGetAccountEvent(repositoryProviderName, alreadyLoggedIn: true), _activityId);
        }

        Accounts = new ObservableCollection<string>(loggedInAccounts.Select(x => x.LoginId));
    }

    /// <summary>
    /// Adds repositories to the list of repos to clone.
    /// Removes repositories from the list of repos to clone.
    /// </summary>
    /// <param name="providerName">The provider that is used to do the cloning.</param>
    /// <param name="accountName">The account used to authenticate into the provider.</param>
    /// <param name="repositoriesToAdd">Repositories to add</param>
    /// <param name="repositoriesToRemove">Repositories to remove.</param>
    /// <remarks>
    /// User has to go through the account screen to get here.  The login id to use is known.
    /// Repos will not be saved when filtering is taking place, or SelectRange is being called.
    /// Both filtering and SelectRange kicks off this event and EverythingToClone should not be altered at this time.
    /// </remarks>
    public void AddOrRemoveRepository(string providerName, string accountName, IList<object> repositoriesToAdd, IList<object> repositoriesToRemove)
    {
        // return right away if this event is fired because of filtering or SelectRange is called.
        if (_isFiltering || IsCallingSelectRange)
        {
            return;
        }

        Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Adding and removing repositories");
        var developerId = _providers.GetAllLoggedInAccounts(providerName).FirstOrDefault(x => x.LoginId == accountName);
        foreach (RepoViewListItem repositoryToRemove in repositoriesToRemove)
        {
            Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Removing repository {repositoryToRemove}");

            var repoToRemove = _repositoriesForAccount.FirstOrDefault(x => x.DisplayName.Equals(repositoryToRemove.RepoName, StringComparison.OrdinalIgnoreCase));
            if (repoToRemove == null)
            {
                continue;
            }

            var cloningInformation = new CloningInformation(repoToRemove);
            cloningInformation.ProviderName = _providers.DisplayName(providerName);
            cloningInformation.OwningAccount = developerId;

            EverythingToClone.Remove(cloningInformation);
        }

        foreach (RepoViewListItem repositoryToAdd in repositoriesToAdd)
        {
            Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Adding repository {repositoryToAdd}");
            var repoToAdd = _repositoriesForAccount.FirstOrDefault(x => x.DisplayName.Equals(repositoryToAdd.RepoName, StringComparison.OrdinalIgnoreCase));
            if (repoToAdd == null)
            {
                continue;
            }

            var cloningInformation = new CloningInformation(repoToAdd);
            cloningInformation.RepositoryProvider = _providers.GetSDKProvider(providerName);
            cloningInformation.ProviderName = _providers.DisplayName(providerName);
            cloningInformation.OwningAccount = developerId;
            cloningInformation.EditClonePathAutomationName = _stringResource.GetLocalized(StringResourceKey.RepoPageEditClonePathAutomationProperties, $"{providerName}/{repositoryToAdd}");
            cloningInformation.RemoveFromCloningAutomationName = _stringResource.GetLocalized(StringResourceKey.RepoPageRemoveRepoAutomationProperties, $"{providerName}/{repositoryToAdd}");
            EverythingToClone.Add(cloningInformation);
        }
    }

    /// <summary>
    /// Validates that url is a valid url and changes url to be absolute if valid.
    /// </summary>
    /// <param name="url">The url to validate</param>
    /// <param name="uri">The Uri after validation.</param>
    /// <remarks>If the url is not valid this method sets UrlParsingError and ShouldShowUrlError to the correct values.</remarks>
    private void ValidateUriAndChangeUiIfBad(string url, out Uri uri)
    {
        // If the url isn't valid don't bother finding a provider.
        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri))
        {
            UrlParsingError = _stringResource.GetLocalized(StringResourceKey.UrlValidationBadUrl);
            ShouldShowUrlError = Visibility.Visible;
            return;
        }

        // If user entered a relative Uri put it into a UriBuilder to turn it into an
        // absolute Uri.  UriBuilder prepends the https scheme
        if (!uri.IsAbsoluteUri)
        {
            try
            {
                var uriBuilder = new UriBuilder(uri.OriginalString);
                uriBuilder.Port = -1;
                uri = uriBuilder.Uri;
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.RepoConfig, $"Invalid URL {uri.OriginalString}", e);
                UrlParsingError = _stringResource.GetLocalized(StringResourceKey.UrlValidationBadUrl);
                ShouldShowUrlError = Visibility.Visible;
                return;
            }
        }

        return;
    }

    /// <summary>
    /// Adds a repository from the URL page. Steps to determine what repoProvider to use.
    /// 1. All providers are asked "Can you parse this into a URL you understand."  If yes, that provider to clone the repo.
    /// 2. If no providers can parse the URL a fall back "GitProvider" is used that uses libgit2sharp to clone the repo.
    /// ShouldShowUrlError is set here.
    /// </summary>
    /// <remarks>
    /// If ShouldShowUrlError == Visible the repo is not added to the list of repos to clone.
    /// </remarks>
    /// <param name="cloneLocation">The location to clone the repo to</param>
    public void AddRepositoryViaUri(string url, string cloneLocation, Frame loginFrame)
    {
        ShouldEnablePrimaryButton = false;
        Uri uri = null;
        ValidateUriAndChangeUiIfBad(url, out uri);

        if (uri == null)
        {
            return;
        }

        // This will return null even if the repo uri has a typo in it.
        // Causing GetCloningInformationFromURL to fall back to git.
        var provider = _providers.CanAnyProviderSupportThisUri(uri);

        var cloningInformation = GetCloningInformationFromUrl(provider, cloneLocation, uri, loginFrame);
        if (cloningInformation == null)
        {
            // Error information is already set.
            // Error string is visible
            return;
        }

        ShouldShowUrlError = Visibility.Collapsed;

        // User could paste in a url of an already added repo.  Check for that here.
        if (_previouslySelectedRepos.Any(x => x.RepositoryToClone.OwningAccountName.Equals(cloningInformation.RepositoryToClone.OwningAccountName, StringComparison.OrdinalIgnoreCase)
            && x.RepositoryToClone.DisplayName.Equals(cloningInformation.RepositoryToClone.DisplayName, StringComparison.OrdinalIgnoreCase)))
        {
            UrlParsingError = _stringResource.GetLocalized(StringResourceKey.UrlValidationRepoAlreadyAdded);
            ShouldShowUrlError = Visibility.Visible;
            Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Repository has already been added.");
            TelemetryFactory.Get<ITelemetry>().LogCritical("RepoTool_RepoAlreadyAdded_Event", false, _activityId);
            return;
        }

        Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Adding repository to clone {cloningInformation.RepositoryId} to location '{cloneLocation}'");

        EverythingToClone.Add(cloningInformation);
        ShouldEnablePrimaryButton = true;
    }

    /// <summary>
    /// Tries to assign a provider to a validated uri.
    /// </summary>
    /// <param name="provider">The provider to test with.</param>
    /// <param name="cloneLocation">The location the user wnats to clone the repo.</param>
    /// <param name="uri">The uri to the repo (Should be a valid uri)</param>
    /// <param name="loginFrame">The frame to show OAUTH login if the user needs to log in.</param>
    /// <returns>non-null cloning information if a provider is selected for cloning.  Null for all other cases.</returns>
    /// <remarks>If the repo is either private, or does not exist, this will ask the user to log in.</remarks>
    private CloningInformation GetCloningInformationFromUrl(RepositoryProvider provider, string cloneLocation, Uri uri, Frame loginFrame)
    {
        if (provider == null)
        {
            // Fallback to a generic git provider.
            // Code path lights up for a repo that has a typo.
            var cloningInformation = new CloningInformation(new GenericRepository(uri));
            cloningInformation.ProviderName = "git";
            cloningInformation.CloningLocation = new DirectoryInfo(cloneLocation);

            return cloningInformation;
        }

        // Repo may be public.  Try that.
        var repo = provider.GetRepositoryFromUri(uri);
        if (repo != null)
        {
            var cloningInformation = new CloningInformation(repo);
            cloningInformation.RepositoryProvider = provider.GetProvider();
            cloningInformation.ProviderName = provider.DisplayName;
            cloningInformation.CloningLocation = new DirectoryInfo(cloneLocation);

            return cloningInformation;
        }

        // Repo may be private, or not exist.  Try to get repo info with all logged in accounts.
        var loggedInAccounts = provider.GetAllLoggedInAccounts();
        if (loggedInAccounts.Any())
        {
            foreach (var loggedInAccount in loggedInAccounts)
            {
                repo = provider.GetRepositoryFromUri(uri, loggedInAccount);
                if (repo != null)
                {
                    var cloningInformation = new CloningInformation(repo);
                    cloningInformation.RepositoryProvider = provider.GetProvider();
                    cloningInformation.ProviderName = provider.DisplayName;
                    cloningInformation.CloningLocation = new DirectoryInfo(cloneLocation);
                    cloningInformation.OwningAccount = loggedInAccount;

                    return cloningInformation;
                }
            }

            // In the case that no logged in accounts can access it, return null
            // until DevHome can handle multiple accounts.
            // Should have a better error string.
            // TODO: Figure out a better error message?
            UrlParsingError = _stringResource.GetLocalized(StringResourceKey.UrlNoAccountsHaveAccess);
            ShouldShowUrlError = Visibility.Visible;

            return null;
        }

        // At this point one of three things are true
        // 1. The repo is private and no accounts are logged in.
        // 2. The repo does not exist (Might have been a typo in the name)
        // Because DevHome cannot tell if a repo is private, or does not exist, prompt the user to log in.
        // Only ask if DevHome hasn't asked already.
        UrlParsingError = _stringResource.GetLocalized(StringResourceKey.UrlNoAccountsHaveAccess);
        ShouldShowUrlError = Visibility.Visible;
        IsLoggingIn = true;
        InitiateAddAccountUserExperienceAsync(provider, loginFrame);
        return null;
    }

    /// <summary>
    /// Launches the login experience for the provided provider.
    /// </summary>
    /// <param name="provider">The provider used to log the user in.</param>
    /// <param name="loginFrame">The frame to use to display the OAUTH path</param>
    private void InitiateAddAccountUserExperienceAsync(RepositoryProvider provider, Frame loginFrame)
    {
        TelemetryFactory.Get<ITelemetry>().Log(
                                                "EntryPoint_DevId_Event",
                                                LogLevel.Critical,
                                                new EntryPointEvent(EntryPointEvent.EntryPoint.Settings));

        provider.SetChangedEvent(_developerIdChangedEvent);
        var authenticationFlow = provider.GetAuthenticationExperienceKind();
        if (authenticationFlow == AuthenticationExperienceKind.CardSession)
        {
            var loginUi = _providers.GetLoginUi(provider.ExtensionDisplayName, SelectedTheme);
            loginFrame.Content = loginUi;
        }
        else if (authenticationFlow == AuthenticationExperienceKind.CustomProvider)
        {
            var windowHandle = _host.GetService<WindowEx>().GetWindowHandle();
            var windowPtr = Win32Interop.GetWindowIdFromWindow(windowHandle);
            try
            {
                var developerIdResult = provider.ShowLogonBehavior(windowPtr).AsTask().Result;
                if (developerIdResult.Result.Status == ProviderOperationStatus.Failure)
                {
                    GlobalLog.Logger?.ReportError($"{developerIdResult.Result.DisplayMessage} - {developerIdResult.Result.DiagnosticText}");
                    return;
                }
            }
            catch (Exception ex)
            {
                GlobalLog.Logger?.ReportError($"Exception thrown while calling show logon session", ex);
            }
        }
    }

    /// <summary>
    /// Gets all the repositories for the specified provider and account.
    /// </summary>
    /// <remarks>
    /// The side effect of this method is _repositoriesForAccount is populated with repositories.
    /// </remarks>
    /// <param name="repositoryProvider">The provider.  This should match the display name of the extension</param>
    /// <param name="loginId">The login Id to get the repositories for</param>
    public async Task GetRepositoriesAsync(string repositoryProvider, string loginId)
    {
        _selectedAccount = loginId;
        IsFetchingRepos = true;
        await Task.Run(() =>
        {
            TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetRepos_Event", LogLevel.Critical, new RepoToolEvent("GettingAllLoggedInAccounts"), _activityId);
            var loggedInDeveloper = _providers.GetAllLoggedInAccounts(repositoryProvider).FirstOrDefault(x => x.LoginId == loginId);

            TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetRepos_Event", LogLevel.Critical, new RepoToolEvent("GettingAllRepos"), _activityId);
            _repositoriesForAccount = _providers.GetAllRepositories(repositoryProvider, loggedInDeveloper);
        });
        IsFetchingRepos = false;
    }

    /// <summary>
    /// Updates the UI with the repositories to display for the specific user and provider.
    /// </summary>
    /// <param name="repositoryProvider">The name of the provider</param>
    /// <param name="loginId">The login ID</param>
    /// <returns>All previously selected repos excluding any added via URL.</returns>
    public IEnumerable<RepoViewListItem> SetRepositories(string repositoryProvider, string loginId)
    {
        Repositories = new ObservableCollection<RepoViewListItem>(OrderRepos(_repositoriesForAccount ?? ImmutableArray<IRepository>.Empty));

        return _previouslySelectedRepos.Where(x => x.OwningAccount != null)
            .Where(x => x.RepositoryProvider.DisplayName.Equals(repositoryProvider, StringComparison.OrdinalIgnoreCase)
            && x.OwningAccount.LoginId.Equals(loginId, StringComparison.OrdinalIgnoreCase))
            .Select(x => new RepoViewListItem(x.RepositoryToClone));
    }

    /// <summary>
    /// Sets the clone location for all repositories to _cloneLocation
    /// </summary>
    /// <param name="cloneLocation">The location to clone all repositories to.</param>
    public void SetCloneLocation(string cloneLocation)
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Setting the clone location for all repositories to {cloneLocation}");
        foreach (var cloningInformation in EverythingToClone)
        {
            cloningInformation.CloningLocation = new DirectoryInfo(cloneLocation);
        }
    }
}
