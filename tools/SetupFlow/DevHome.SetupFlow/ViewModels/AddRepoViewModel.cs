// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Controls;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.DeveloperId;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.Contracts.Services;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.Views;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
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
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(AddRepoViewModel));

    private readonly IHost _host;

    private readonly Guid _activityId;

    private readonly ISetupFlowStringResource _stringResource;

    private readonly SetupFlowOrchestrator _setupFlowOrchestrator;

    private readonly List<CloningInformation> _previouslySelectedRepos;

    private readonly DispatcherQueue _dispatcherQueue;

    /// <summary>
    /// Holds all the currently executing tasks to GetRepositories.
    /// Used to match a Task against _taskToUseForResults to make sure the results of the most recently executed task
    /// is shows in the UI.
    /// </summary>
    private readonly List<Task> _runningGetReposTasks = new();

    /// <summary>
    /// Because logic is split between the back-end and the view model, migrating code from the view
    /// in one PR to the view model is too much work.
    /// This member is here to support this partial migration.  Once all the code-behind logic is out of the view
    /// _addRepoDialog can be removed.
    /// </summary>
    /// <remarks>
    /// This is only to help reference back to the view's code-behind.  Please do not change anything inside
    /// this class.
    /// </remarks>
    private readonly AddRepoDialog _addRepoDialog;

    private readonly object _setRepositoriesLock = new();

    private List<RepoViewListItem> _allRepositories = new();

    /// <summary>
    /// Hold the task of the most recently ran GetRepos request.
    /// </summary>
    private Task _taskToUseForResults;

    /// <summary>
    /// Used to store the search fields and their values when querying for repos.
    /// </summary>
    private Dictionary<string, string> _repoSearchInputs = new();

    /// <summary>
    /// Gets the folder picker view model.
    /// </summary>
    /// <remarks>
    /// Currently public because EditDevDriveViewModel needs access to it.
    /// THis can be made private when EditDevDriveViewModel is in this class.
    /// </remarks>
    public FolderPickerViewModel FolderPickerViewModel
    {
        get; private set;
    }

    /// <summary>
    /// Gets the view model to handle adding a dev drive.
    /// </summary>
    public EditDevDriveViewModel EditDevDriveViewModel
    {
        get; private set;
    }

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
    private ObservableCollection<string> _providerNames = new();

    /// <summary>
    /// Names of all accounts the user has logged into for a particular provider.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _accounts = new();

    /// <summary>
    /// The currently selected account.
    /// </summary>
    [ObservableProperty]
    private string _selectedAccount;

    /// <summary>
    /// All repositories currently shown on the screen.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<RepoViewListItem> _repositoriesToDisplay = new();

    /// <summary>
    /// Should the URL page be visible?
    /// </summary>
    [ObservableProperty]
    private bool _showUrlPage;

    /// <summary>
    /// Should the account page be visible?
    /// </summary>
    [ObservableProperty]
    private bool _showAccountPage;

    /// <summary>
    /// Should the repositories page be visible?
    /// </summary>
    [ObservableProperty]
    private bool _showRepoPage;

    /// <summary>
    /// If the extension implements IRepositoryProvider2 users can navigate to this page
    /// allowing users to define a simple search query to narrow down the repos returned from the extension.
    /// </summary>
    [ObservableProperty]
    private bool _shouldShowSelectingSearchTerms;

    /// <summary>
    /// Should the error text be shown?
    /// </summary>
    [ObservableProperty]
    private bool _showErrorTextBox;

    /// <summary>
    /// Keeps track of if the account button is checked.  Used to switch UIs
    /// </summary>
    [ObservableProperty]
    private bool? _isAccountToggleButtonChecked;

    /// <summary>
    /// Possible the user is not logged in.  In that case, disable the account button.
    /// </summary>
    [ObservableProperty]
    private bool _isAccountButtonEnabled;

    /// <summary>
    /// Keeps track if the URL button is checked.  Used to switch UIs
    /// </summary>
    [ObservableProperty]
    private bool? _isUrlAccountButtonChecked;

    /// <summary>
    /// The text of the primary button is different on different pages.
    /// </summary>
    [ObservableProperty]
    private string _primaryButtonText;

    /// <summary>
    /// The string to show the user if the url can't be parsed.
    /// </summary>
    [ObservableProperty]
    private string _urlParsingError;

    /// <summary>
    /// If the URL parsing error should be shown.
    /// </summary>
    [ObservableProperty]
    private bool _shouldShowUrlError;

    /// <summary>
    /// If DevHome is getting repos from the extension.
    /// Used to change the UI.
    /// </summary>
    [ObservableProperty]
    private bool _isFetchingRepos;

    /// <summary>
    /// PRimary button should not be enabled if not all information is entered.
    /// </summary>
    [ObservableProperty]
    private bool _shouldEnablePrimaryButton;

    /// <summary>
    /// Depending on the page shown, the primary button style will be different.
    /// </summary>
    [ObservableProperty]
    private Style _styleForPrimaryButton;

    /// <summary>
    /// If a UI should be shown to ask theuser to log in.
    /// </summary>
    [ObservableProperty]
    private bool _shouldShowLoginUi;

    /// <summary>
    /// For some log in scenarios, no in-house cancel button is on the UI.
    /// In that case, add our own.
    /// </summary>
    [ObservableProperty]
    private bool _shouldShowXButtonInLoginUi;

    /// <summary>
    /// DevHome waits when a UI prompt is open.  This is used to exit the wait
    /// early if the user cancel the log in.
    /// </summary>
    [ObservableProperty]
    private bool _isCancelling;

    /// <summary>
    /// What to display to the left of the combobox.
    /// </summary>
    [ObservableProperty]
    private string _selectionOptionsPrefix;

    /// <summary>
    /// The options a user can pick from for a granular search.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _selectionOptions;

    /// <summary>
    /// The placeholder text for the selection options combobox
    /// </summary>
    [ObservableProperty]
    private string _selectionOptionsPlaceholderText;

    /// <summary>
    /// Used to figure out what button is pressed for the split button.
    /// This determines the UI elements shown/hidden.
    /// </summary>
    private enum SegmentedItemTag
    {
        Account,
        URL,
    }

    /// <summary>
    /// Hides/Shows UI elements for the selected button.
    /// </summary>
    /// <param name="selectedItem">The button the user clicked on.</param>
    [RelayCommand]
    public async Task ChangePage(SegmentedItem selectedItem)
    {
        if (selectedItem.Tag == null)
        {
            return;
        }

        if (!Enum.TryParse<SegmentedItemTag>(selectedItem.Tag.ToString(), out var pageToGoTo))
        {
            return;
        }

        if (pageToGoTo == SegmentedItemTag.Account)
        {
            await ChangeToAccountPageAsync();
            return;
        }

        if (pageToGoTo == SegmentedItemTag.URL)
        {
            ChangeToUrlPage();
            return;
        }

        // enum did not match.  Don't change.
        return;
    }

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

    public bool IsSettingUpLocalMachine => _setupFlowOrchestrator.IsSettingUpLocalMachine;

    private TypedEventHandler<IDeveloperIdProvider, IDeveloperId> _developerIdChangedEvent;

    private string _selectedRepoProvider = string.Empty;

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

        _selectedRepoProvider = repositoryProviderName;
    }

    [RelayCommand]
    private void CancelButtonPressed()
    {
        IsLoggingIn = false;
        IsCancelling = true;
    }

    /// <summary>
    /// The accounts the user is logged into is stored here.
    /// </summary>
    [ObservableProperty]
    private MenuFlyout _accountsToShow;

    /// <summary>
    /// Used to show the login UI.
    /// </summary>
    [ObservableProperty]
    private Frame _loginUiContent;

    /// <summary>
    /// Soley used to reset the account drop down when the account page is navigated to.
    /// </summary>
    [ObservableProperty]
    private int _accountIndex;

    /// <summary>
    /// Text that prompts the user if they want to add search inputs.
    /// </summary>
    [ObservableProperty]
    private string _askToChangeLabel;

    /// <summary>
    /// If the extension allows users to further filter repo results.
    /// </summary>
    [ObservableProperty]
    private bool _shouldShowGranularSearch;

    /// <summary>
    /// Controls if the hyperlink button that allows switching to the search terms page is visible.
    /// </summary>
    [ObservableProperty]
    private bool _shouldShowChangeSearchTermsHyperlinkButton;

    /// <summary>
    /// Switches the repos shown to the account selected.
    /// </summary>
    [RelayCommand]
    private void MenuItemClick(string selectedItemName)
    {
        _dispatcherQueue.TryEnqueue(async () =>
        {
            SelectedAccount = selectedItemName;
            await GetRepositoriesAsync(_selectedRepoProvider, SelectedAccount);

            var sdkDisplayName = _providers.GetSDKProvider(_selectedRepoProvider).DisplayName;
            _addRepoDialog.SelectRepositories(SetRepositories(sdkDisplayName, SelectedAccount));
        });
    }

    /// <summary>
    /// Uses search inputs to search for repos.
    /// </summary>
    private void SearchRepos()
    {
        _dispatcherQueue.TryEnqueue(async () =>
        {
            await SearchForRepos(_selectedRepoProvider, SelectedAccount);

            var sdkDisplayName = _providers.GetSDKProvider(_selectedRepoProvider).DisplayName;
            _addRepoDialog.SelectRepositories(SetRepositories(sdkDisplayName, SelectedAccount));
        });
    }

    [RelayCommand]
    private async Task OpenFolderPicker()
    {
        await FolderPickerViewModel.ChooseCloneLocation();
        ToggleCloneButton();
    }

    /// <summary>
    /// If granular search is enabled, this method handles the "SelectionChanged" event on the
    /// combo box.
    /// </summary>
    /// <param name="selectedItem">The selection option the user chose.</param>
    [RelayCommand]
    private void SelectionOptionsChanged(string selectedItem)
    {
        if (selectedItem == null)
        {
            return;
        }

        List<RepoViewListItem> reposWithPathPart = new();
        foreach (var repo in _allRepositories)
        {
            var pathParts = repo.OwningAccountName.Split(Path.DirectorySeparatorChar);
            var partToCompareAgainst = pathParts[pathParts.Length - 1];
#pragma warning disable CA1309 // Use ordinal string comparison
            if (selectedItem.Equals(partToCompareAgainst))
            {
                reposWithPathPart.Add(repo);
            }
#pragma warning restore CA1309 // Use ordinal string comparison
        }

        RepositoriesToDisplay = new ObservableCollection<RepoViewListItem>(reposWithPathPart);
    }

    /// <summary>
    /// The bottom of the MenuFlyout has a button to log into another account.  Handle logging the user in.
    /// </summary>
    /// <remarks>
    /// This calls MenuItemClick to poulate the list of repos if a new account is detected.
    /// </remarks>
    [RelayCommand]
    private async Task AddAccountClicked()
    {
        // If the user selects repos from account 1, then logs into account 2 and does not save between those two actions
        // _previouslySelectedRepos will be empty.  The result is the repos in account 1 will not be selected if the user navigates
        // to account 1 after logging into account 2.
        // Save the repos here in that case.
        if (_previouslySelectedRepos.Count == 0)
        {
            _previouslySelectedRepos.AddRange(EverythingToClone);
        }

        ShowRepoPage = false;

        // Store the logged in accounts to help figure out what account the user logged into.
        var loggedInAccounts = await Task.Run(() => _providers.GetAllLoggedInAccounts(_selectedRepoProvider));
        await LogUserIn(_selectedRepoProvider, LoginUiContent, true);
        var loggedInAccountsWithNewAccount = await Task.Run(() => _providers.GetAllLoggedInAccounts(_selectedRepoProvider));

        ShowRepoPage = true;
        Accounts = new ObservableCollection<string>(loggedInAccountsWithNewAccount.Select(x => x.LoginId));
        AccountsToShow = ConstructFlyout();

        // The dialog makes a user log in if they have no accounts.
        // keep this here just in case.
        if (Accounts.Any())
        {
            var newAccount = loggedInAccountsWithNewAccount.Except(loggedInAccounts);

            // Logging in should allow only one account to log in at a time.
            if (newAccount.Count() > 1)
            {
                _log.Error($"{newAccount.Count()} accounts logged in at once.  Choosing the first alphabetically");
            }

            if (newAccount.Any())
            {
                SelectedAccount = newAccount.OrderByDescending(x => x.LoginId).FirstOrDefault().LoginId;
            }
            else
            {
                SelectedAccount = Accounts.First();
            }

            IsCancelling = false;
            var firstItem = AccountsToShow.Items.FirstOrDefault(x => x.Name.Equals(SelectedAccount, StringComparison.OrdinalIgnoreCase));
            MenuItemClick((firstItem as MenuFlyoutItem).Text);
        }
    }

    /// <summary>
    /// Filters all repos down to any that start with text.
    /// A side-effect of filtering is that SelectionChanged fires for every selected repo but only on removal.
    /// SelectionChanged isn't fired for re-adding because repos are removed, not added.  To prevent the RepoTool from forgetting the repos that were selected
    /// the flag _isFiltering is used to prevent modifications to EverythingToClone.
    /// Once filtering is done SelectRange is called on each item in EverythingToClone to re-select them.
    /// </summary>
    /// <param name="text">The text to use with .Contains</param>
    public void FilterRepositories(string text)
    {
        IEnumerable<RepoViewListItem> filteredRepositories;
        if (text.Equals(string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            filteredRepositories = _allRepositories;
        }
        else
        {
            filteredRepositories = _allRepositories
                .Where(x => x.RepoDisplayName.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        _isFiltering = true;
        RepositoriesToDisplay = new ObservableCollection<RepoViewListItem>(filteredRepositories);
        _isFiltering = false;
    }

    /// <summary>
    /// Makes the MenuFlyout object used to display multple accounts in the repo tool.
    /// </summary>
    /// <returns>The MenuFlyout to display.</returns>
    /// <remarks>
    /// The layout is a list of added accounts.  A line seperator.  One menu item to add an account.
    /// </remarks>
    private MenuFlyout ConstructFlyout()
    {
        var newMenu = new MenuFlyout();
        foreach (var account in Accounts)
        {
            var accountMenuItem = new MenuFlyoutItem();
            accountMenuItem.Name = account;
            accountMenuItem.Text = account;
            accountMenuItem.Command = MenuItemClickCommand;
            accountMenuItem.CommandParameter = accountMenuItem.Text;
            newMenu.Items.Add(accountMenuItem);
        }

        newMenu.Items.Add(new MenuFlyoutSeparator());
        var addAccountMenuItem = new MenuFlyoutItem();
        addAccountMenuItem.Text = _stringResource.GetLocalized("RepoToolAddAnotherAccount");
        addAccountMenuItem.Command = AddAccountClickedCommand;
        newMenu.Items.Add(addAccountMenuItem);

        return newMenu;
    }

    public AddRepoViewModel(
        SetupFlowOrchestrator setupFlowOrchestrator,
        ISetupFlowStringResource stringResource,
        List<CloningInformation> previouslySelectedRepos,
        IHost host,
        Guid activityId,
        AddRepoDialog addRepoDialog,
        IDevDriveManager devDriveManager)
    {
        _addRepoDialog = addRepoDialog;
        _stringResource = stringResource;
        _host = host;
        _dispatcherQueue = host.GetService<WindowEx>().DispatcherQueue;
        _loginUiContent = new Frame();
        _setupFlowOrchestrator = setupFlowOrchestrator;

        _previouslySelectedRepos = previouslySelectedRepos ?? new List<CloningInformation>();
        EverythingToClone = new List<CloningInformation>(_previouslySelectedRepos);
        _activityId = activityId;
        FolderPickerViewModel = new FolderPickerViewModel(stringResource, setupFlowOrchestrator);
        EditDevDriveViewModel = new EditDevDriveViewModel(devDriveManager, setupFlowOrchestrator);

        EditDevDriveViewModel.DevDriveClonePathUpdated += (_, updatedDevDriveRootPath) =>
        {
            FolderPickerViewModel.CloneLocationAlias = EditDevDriveViewModel.GetDriveDisplayName(DevDriveDisplayNameKind.FormattedDriveLabelKind);
            FolderPickerViewModel.CloneLocation = updatedDevDriveRootPath;
        };

        ChangeToUrlPage();

        // override changes ChangeToUrlPage to correctly set the state.
        UrlParsingError = string.Empty;
        ShouldShowUrlError = false;
        ShowErrorTextBox = false;
        _accountIndex = -1;
    }

    /// <summary>
    /// Toggles the clone button.  Make sure other view models have correct information.
    /// </summary>
    public void ToggleCloneButton()
    {
        var isEverythingGood = ValidateRepoInformation() && FolderPickerViewModel.ValidateCloneLocation();
        if (EditDevDriveViewModel.DevDrive != null && EditDevDriveViewModel.DevDrive.State != DevDriveState.ExistsOnSystem)
        {
            isEverythingGood &= EditDevDriveViewModel.IsDevDriveValid();
        }

        ShouldEnablePrimaryButton = isEverythingGood;

        // Fill in EverythingToClone with the location
        if (isEverythingGood)
        {
            SetCloneLocation(FolderPickerViewModel.CloneLocation);
        }
    }

    /// <summary>
    /// Gets all the extensions the DevHome can see.
    /// </summary>
    /// <remarks>
    /// A valid extension is one that has a repository provider and developerId provider.
    /// </remarks>
    public void GetExtensions()
    {
        // Don't use the repository extensions if we are in the setup target flow.
        if (_setupFlowOrchestrator.IsSettingUpATargetMachine)
        {
            return;
        }

        _log.Information("Getting installed extensions with Repository and DevId providers");
        var extensionService = _host.GetService<IExtensionService>();
        var extensionWrappers = extensionService.GetInstalledExtensionsAsync().Result;

        var extensions = extensionWrappers.Where(
            extension => extension.HasProviderType(ProviderType.Repository) &&
            extension.HasProviderType(ProviderType.DeveloperId)).OrderBy(extensionWrapper => extensionWrapper.Name);

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
        FolderPickerViewModel.ShowFolderPicker();
        EditDevDriveViewModel.ShowDevDriveUIIfEnabled();
        _log.Information("Changing to Url page");
        ShowUrlPage = true;
        ShowAccountPage = false;
        ShowRepoPage = false;
        IsUrlAccountButtonChecked = true;
        IsAccountToggleButtonChecked = false;
        CurrentPage = PageKind.AddViaUrl;
        PrimaryButtonText = _stringResource.GetLocalized(StringResourceKey.RepoEverythingElsePrimaryButtonText);
        ShouldShowLoginUi = false;

        ToggleCloneButton();
    }

    public async Task ChangeToAccountPageAsync()
    {
        AccountIndex = -1;

        // List of extensions needs to be refreshed before accessing
        GetExtensions();
        if (ProviderNames.Count == 1)
        {
            _selectedRepoProvider = ProviderNames[0];
            _providers.StartIfNotRunning(ProviderNames[0]);
            var accounts = _providers.GetAllLoggedInAccounts(ProviderNames[0]);
            if (accounts.Count() == 1)
            {
                CanSkipAccountConnection = true;
            }
        }

        FolderPickerViewModel.CloseFolderPicker();
        EditDevDriveViewModel.HideDevDriveUI();

        // If DevHome has 1 provider installed and the provider has 1 logged in account
        // switch to the repo page.
        if (CanSkipAccountConnection)
        {
            await ChangeToRepoPageAsync();
            return;
        }

        _log.Information("Changing to Account page");
        ShouldShowUrlError = false;
        ShowUrlPage = false;
        ShowAccountPage = true;
        ShowRepoPage = false;
        IsUrlAccountButtonChecked = false;
        IsAccountToggleButtonChecked = true;
        CurrentPage = PageKind.AddViaAccount;
        PrimaryButtonText = _stringResource.GetLocalized(StringResourceKey.RepoAccountPagePrimaryButtonText);
        ShouldShowLoginUi = false;
        ToggleCloneButton();
    }

    public async Task ChangeToRepoPageAsync()
    {
        await GetAccountsAsync(_selectedRepoProvider, LoginUiContent);
        if (Accounts.Any())
        {
            FolderPickerViewModel.ShowFolderPicker();
            EditDevDriveViewModel.ShowDevDriveUIIfEnabled();
            SelectedAccount = Accounts.First();
            ShouldEnablePrimaryButton = false;
            MenuItemClick((AccountsToShow.Items[0] as MenuFlyoutItem).Text);
        }

        _log.Information("Changing to Repo page");
        ShowUrlPage = false;
        ShowAccountPage = false;
        ShowRepoPage = true;

        ShouldShowSelectingSearchTerms = false;
        ShouldShowGranularSearch = false;
        ShouldShowChangeSearchTermsHyperlinkButton = _providers.IsSearchingEnabled(_selectedRepoProvider);
        AskToChangeLabel = _providers.GetAskChangeSearchFieldsLabel(_selectedRepoProvider);

        CurrentPage = PageKind.Repositories;
        PrimaryButtonText = _stringResource.GetLocalized(StringResourceKey.RepoEverythingElsePrimaryButtonText);
        ShouldShowLoginUi = false;

        // The only way to get the repo page is through the account page.
        // No need to toggle the clone button.
    }

    /// <summary>
    /// Sends out a request to search for repos using searchInputs.
    /// </summary>
    /// <param name="searchInputs">The values to search for repos with.</param>
    public void SearchForRepos(Dictionary<string, string> searchInputs)
    {
        _repoSearchInputs = searchInputs;
        SearchRepos();
    }

    public void ChangeToSelectSearchTermsPage()
    {
        CurrentPage = PageKind.SearchFields;
        IsFetchingRepos = false;
        _log.Information("Changing to select search terms page");
        ShowUrlPage = false;
        ShowAccountPage = false;
        ShowRepoPage = false;
        ShouldShowSelectingSearchTerms = true;
        FolderPickerViewModel.ShouldShowFolderPicker = false;
        EditDevDriveViewModel.ShowDevDriveInformation = false;
        PrimaryButtonText = "Connect";
        ShouldEnablePrimaryButton = true;
    }

    /// <summary>
    /// Asks the provider for search terms for querying repositories.
    /// </summary>
    /// <param name="providerName">The provider to ask</param>
    /// <returns>The names of the search fields.</returns>
    public List<string> GetSearchTerms()
    {
        return _providers.GetSearchTerms(_selectedRepoProvider);
    }

    /// <summary>
    /// Asks the provider for a list of suggestions, given values of other search terms.
    /// </summary>
    /// <param name="loginId">The account of the user</param>
    /// <param name="inputFields">All information found in the search grid</param>
    /// <param name="fieldName">The field to request data for</param>
    /// <remarks>
    /// uses _selectedRepoProvider.
    /// </remarks>
    /// <returns>A list of names that can be used for the field.</returns>
    public List<string> GetSuggestionsFor(string loginId, Dictionary<string, string> inputFields, string fieldName)
    {
        var loggedInDeveloper = _providers.GetAllLoggedInAccounts(_selectedRepoProvider).FirstOrDefault(x => x.LoginId == loginId);

        return _providers.GetValuesFor(_selectedRepoProvider, loggedInDeveloper, inputFields, fieldName);
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
                ShouldShowUrlError = true;
                return false;
            }

            var sshMatch = Regex.Match(Url, "^.*@.*:.*\\/.*");

            if (sshMatch.Success)
            {
                UrlParsingError = _stringResource.GetLocalized(StringResourceKey.SSHConnectionStringNotAllowed);
                ShouldShowUrlError = true;
                return false;
            }

            ShouldShowUrlError = false;
            return true;
        }
        else if (CurrentPage == PageKind.AddViaAccount || CurrentPage == PageKind.Repositories)
        {
            return EverythingToClone.Count > 0;
        }
        else if (CurrentPage == PageKind.SearchFields)
        {
            // IRepositoryProvider2 does not impose a structure to the search terms.
            // Any combination of search terms, including empty, is accepted.
            return true;
        }
        else
        {
            return false;
        }
    }

    private async Task LogUserIn(string repositoryProviderName, Frame loginFrame, bool shouldShowXCancelButton = false)
    {
        IsLoggingIn = true;
        ShouldShowLoginUi = true;

        // AddRepoDialog can handle the close button click.  Don't show the x button.
        ShouldShowXButtonInLoginUi = shouldShowXCancelButton;
        await InitiateAddAccountUserExperienceAsync(_providers.GetProvider(repositoryProviderName), loginFrame);

        // Wait 30 seconds for user to log in.
        var maxIterationsToWait = 30;
        var currentIteration = 0;
        var waitDelay = Convert.ToInt32(new TimeSpan(0, 0, 1).TotalMilliseconds);
        while ((IsLoggingIn && !IsCancelling) && currentIteration++ <= maxIterationsToWait)
        {
            await Task.Delay(waitDelay);
        }

        ShouldShowLoginUi = false;
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
            await LogUserIn(repositoryProviderName, loginFrame);
            loggedInAccounts = await Task.Run(() => _providers.GetAllLoggedInAccounts(repositoryProviderName));
            TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetAccount_Event", LogLevel.Critical, new RepoDialogGetAccountEvent(repositoryProviderName, alreadyLoggedIn: false), _activityId);
        }
        else
        {
            TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetAccount_Event", LogLevel.Critical, new RepoDialogGetAccountEvent(repositoryProviderName, alreadyLoggedIn: true), _activityId);
        }

        // At least with the github extension, LoginId is the account name and does not include
        // @github.com.  I could try parsing the host of the URL and append that to the login id.
        // But, if other extensions included the @something.com to the loginid, the solution mentioned above
        // would produce [username]@[something.com]@[something.com].  Not good.
        // To avoid this, just store the login id.
        Accounts = new ObservableCollection<string>(loggedInAccounts.Select(x => x.LoginId));
        AccountsToShow = ConstructFlyout();
    }

    /// <summary>
    /// Adds repositories to the list of repos to clone.
    /// Removes repositories from the list of repos to clone.
    /// </summary>
    /// <param name="accountName">The account used to authenticate into the provider.</param>
    /// <param name="repositoriesToAdd">Repositories to add</param>
    /// <param name="repositoriesToRemove">Repositories to remove.</param>
    /// <remarks>
    /// User has to go through the account screen to get here.  The login id to use is known.
    /// Repos will not be saved when filtering is taking place, or SelectRange is being called.
    /// Both filtering and SelectRange kicks off this event and EverythingToClone should not be altered at this time.
    /// </remarks>
    public void AddOrRemoveRepository(string accountName, IList<object> repositoriesToAdd, IList<object> repositoriesToRemove)
    {
        // return right away if this event is fired because of filtering or SelectRange is called.
        if (_isFiltering || IsCallingSelectRange)
        {
            return;
        }

        _log.Information($"Adding and removing repositories");
        var developerId = _providers.GetAllLoggedInAccounts(_selectedRepoProvider).FirstOrDefault(x => x.LoginId == accountName);
        foreach (RepoViewListItem repositoryToRemove in repositoriesToRemove)
        {
            _log.Information($"Removing repository {repositoryToRemove}");

            var repoToRemove = _repositoriesForAccount.FirstOrDefault(x => x.DisplayName.Equals(repositoryToRemove.RepoName, StringComparison.OrdinalIgnoreCase));
            if (repoToRemove == null)
            {
                continue;
            }

            var cloningInformation = new CloningInformation(repoToRemove);
            cloningInformation.ProviderName = _providers.DisplayName(_selectedRepoProvider);
            cloningInformation.OwningAccount = developerId;

            EverythingToClone.Remove(cloningInformation);
        }

        foreach (RepoViewListItem repositoryToAdd in repositoriesToAdd)
        {
            _log.Information($"Adding repository {repositoryToAdd}");
            var repoToAdd = _repositoriesForAccount.FirstOrDefault(x => x.DisplayName.Equals(repositoryToAdd.RepoName, StringComparison.OrdinalIgnoreCase));
            if (repoToAdd == null)
            {
                continue;
            }

            var cloningInformation = new CloningInformation(repoToAdd);
            cloningInformation.RepositoryProvider = _providers.GetSDKProvider(_selectedRepoProvider);
            cloningInformation.ProviderName = _providers.DisplayName(_selectedRepoProvider);
            cloningInformation.OwningAccount = developerId;
            cloningInformation.EditClonePathAutomationName = _stringResource.GetLocalized(StringResourceKey.RepoPageEditClonePathAutomationProperties, $"{_selectedRepoProvider}/{repositoryToAdd}");
            cloningInformation.RemoveFromCloningAutomationName = _stringResource.GetLocalized(StringResourceKey.RepoPageRemoveRepoAutomationProperties, $"{_selectedRepoProvider}/{repositoryToAdd}");
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
        uri = null;

        // If the url isn't valid don't bother finding a provider.
        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute) ||
            !Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri))
        {
            UrlParsingError = _stringResource.GetLocalized(StringResourceKey.UrlValidationBadUrl);
            ShouldShowUrlError = true;
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
                _log.Error($"Invalid URL {uri.OriginalString}", e);
                UrlParsingError = _stringResource.GetLocalized(StringResourceKey.UrlValidationBadUrl);
                ShouldShowUrlError = true;
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
    public void AddRepositoryViaUri(string url, string cloneLocation)
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
        var provider = _providers?.CanAnyProviderSupportThisUri(uri);

        var cloningInformation = GetCloningInformationFromUrl(provider, cloneLocation, uri, LoginUiContent);
        if (cloningInformation == null)
        {
            // Error information is already set.
            // Error string is visible
            return;
        }

        ShouldShowUrlError = false;

        // User could paste in a url of an already added repo.  Check for that here.
        if (_previouslySelectedRepos.Any(x => x.RepositoryToClone.OwningAccountName.Equals(cloningInformation.RepositoryToClone.OwningAccountName, StringComparison.OrdinalIgnoreCase)
            && x.RepositoryToClone.DisplayName.Equals(cloningInformation.RepositoryToClone.DisplayName, StringComparison.OrdinalIgnoreCase)))
        {
            UrlParsingError = _stringResource.GetLocalized(StringResourceKey.UrlValidationRepoAlreadyAdded);
            ShouldShowUrlError = true;
            _log.Information("Repository has already been added.");
            TelemetryFactory.Get<ITelemetry>().LogCritical("RepoTool_RepoAlreadyAdded_Event", false, _activityId);
            return;
        }

        _log.Information($"Adding repository to clone {cloningInformation.RepositoryId} to location '{cloneLocation}'");

        EverythingToClone.Add(cloningInformation);
        ShouldEnablePrimaryButton = true;
        ShouldShowUrlError = false;
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
            ShouldShowUrlError = true;

            _host.GetService<WindowEx>().DispatcherQueue.TryEnqueue(async () =>
            {
                await InitiateAddAccountUserExperienceAsync(provider, loginFrame);
            });
            return null;
        }

        // At this point one of three things are true
        // 1. The repo is private and no accounts are logged in.
        // 2. The repo does not exist (Might have been a typo in the name)
        // Because DevHome cannot tell if a repo is private, or does not exist, prompt the user to log in.
        // Only ask if DevHome hasn't asked already.
        UrlParsingError = _stringResource.GetLocalized(StringResourceKey.UrlNoAccountsHaveAccess);
        ShouldShowUrlError = true;
        IsLoggingIn = true;
        _host.GetService<WindowEx>().DispatcherQueue.TryEnqueue(async () =>
        {
            await InitiateAddAccountUserExperienceAsync(provider, loginFrame);
        });
        return null;
    }

    /// <summary>
    /// Sets up the UI for dev drives.
    /// </summary>
    public async Task SetupDevDrivesAsync()
    {
        await Task.Run(() =>
        {
            EditDevDriveViewModel.SetUpStateIfDevDrivesIfExists();

            if (EditDevDriveViewModel.DevDrive != null &&
                EditDevDriveViewModel.DevDrive.State == DevDriveState.ExistsOnSystem)
            {
                FolderPickerViewModel.InDevDriveScenario = true;
                EditDevDriveViewModel.ClonePathUpdated();
            }
        });
    }

    /// <summary>
    /// Launches the login experience for the provided provider.
    /// </summary>
    /// <param name="provider">The provider used to log the user in.</param>
    /// <param name="loginFrame">The frame to use to display the OAUTH path</param>
    private async Task InitiateAddAccountUserExperienceAsync(RepositoryProvider provider, Frame loginFrame)
    {
        TelemetryFactory.Get<ITelemetry>().Log(
                                                "EntryPoint_DevId_Event",
                                                LogLevel.Critical,
                                                new EntryPointEvent(EntryPointEvent.EntryPoint.Settings));

        provider.SetChangedEvent(_developerIdChangedEvent);
        var authenticationFlow = provider.GetAuthenticationExperienceKind();
        if (authenticationFlow == AuthenticationExperienceKind.CardSession)
        {
            var loginUi = await _providers.GetLoginUiAsync(provider.ExtensionDisplayName);
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
                    _log.Error($"{developerIdResult.Result.DisplayMessage} - {developerIdResult.Result.DiagnosticText}");
                    return;
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Exception thrown while calling show logon session", ex);
            }
        }
    }

    /// <summary>
    /// Starts the task to search for repos.
    /// </summary>
    /// <param name="repositoryProvider">The name of the selected repository Provider.</param>
    /// <param name="loginId">The loginId of the user.</param>
    /// <returns>An awaitable task.</returns>
    private Task<RepositorySearchInformation> StartSearchingForRepos(string repositoryProvider, string loginId)
    {
        return Task.Run(
              () =>
              {
                  TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetRepos_Event", LogLevel.Critical, new RepoToolEvent("GettingAllLoggedInAccounts"), _activityId);
                  var loggedInDeveloper = _providers.GetAllLoggedInAccounts(repositoryProvider).FirstOrDefault(x => x.LoginId == loginId);

                  TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetRepos_Event", LogLevel.Critical, new RepoToolEvent("GettingAllRepos"), _activityId);
                  return _providers.SearchForRepos(repositoryProvider, loggedInDeveloper, _repoSearchInputs);
              });
    }

    /// <summary>
    /// Starts the task to get all repos.
    /// </summary>
    /// <param name="repositoryProvider">The name of the selected repository Provider.</param>
    /// <param name="loginId">The loginId of the user.</param>
    /// <returns>An awaitable task.</returns>
    private Task<RepositorySearchInformation> StartGettingAllRepos(string repositoryProvider, string loginId)
    {
        return Task.Run(
      () =>
      {
          TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetRepos_Event", LogLevel.Critical, new RepoToolEvent("GettingAllLoggedInAccounts"), _activityId);
          var loggedInDeveloper = _providers.GetAllLoggedInAccounts(repositoryProvider).FirstOrDefault(x => x.LoginId == loginId);

          TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetRepos_Event", LogLevel.Critical, new RepoToolEvent("GettingAllRepos"), _activityId);
          return _providers.GetAllRepositories(repositoryProvider, loggedInDeveloper);
      });
    }

    /// <summary>
    /// Takes a task of getting repositories and makes sure only the most recent request is used.
    /// </summary>
    /// <param name="loginId">The loginId of the user</param>
    /// <param name="runningTask">The running task that is getting repos.</param>
    /// <returns>An awaitable task.</returns>
    private async Task CoordinateTasks(string loginId, Task<RepositorySearchInformation> runningTask)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            SelectedAccount = loginId;
            IsFetchingRepos = true;
        });

        // Multiple calls can execute at the same time.  DevHome uses the results of the
        // most recent query.  A list of tasks is used to keep track of all running queries.
        // When a query is done, it is compared with the id of the most recently executed task.
        // if a match, DevHome uses that.
        // Using locks here to control access to non-thread safe collections.
        lock (_setRepositoriesLock)
        {
            _taskToUseForResults = runningTask;
            _runningGetReposTasks.Add(runningTask);
        }

        await runningTask;
        RepositorySearchInformation repoSearchInformation;
        lock (_setRepositoriesLock)
        {
            _runningGetReposTasks.Remove(runningTask);
            if (runningTask.Id != _taskToUseForResults.Id)
            {
                _repositoriesForAccount ??= new List<IRepository>();
                return;
            }

            repoSearchInformation = runningTask.Result;
            _repositoriesForAccount = repoSearchInformation.Repositories;
            try
            {
                _allRepositories = repoSearchInformation.Repositories.Select(x => new RepoViewListItem(x)).ToList();
            }
            catch (Exception ex)
            {
                _log.Error($"Exception thrown while selecting repositories from the return object", ex);
                _allRepositories = new();
            }
        }

        // Update the UI.
        _dispatcherQueue.TryEnqueue(() =>
        {
            ShouldShowGranularSearch = DoesTheExtensionUseGranularSearch(repoSearchInformation);
            SelectionOptionsPrefix = repoSearchInformation.SelectionOptionsLabel;
            SelectionOptions = new ObservableCollection<string>(repoSearchInformation.SelectionOptions);
            SelectionOptionsPlaceholderText = repoSearchInformation.SelectionOptionsPlaceHolderText;

            IsFetchingRepos = false;
        });
    }

    private bool DoesTheExtensionUseGranularSearch(RepositorySearchInformation repoSearchInformation)
    {
        return !string.IsNullOrEmpty(repoSearchInformation.SelectionOptionsLabel) &&
                        !string.IsNullOrEmpty(repoSearchInformation.SelectionOptionsPlaceHolderText) &&
                        repoSearchInformation.SelectionOptions.Count != 0;
    }

    public async Task SearchForRepos(string repositoryProvider, string loginId)
    {
        var localTask = StartSearchingForRepos(repositoryProvider, loginId);
        await CoordinateTasks(loginId, localTask);
    }

    /// <summary>
    /// Gets all the repositories for the specified provider and account.
    /// </summary>
    /// <remarks>
    /// The side effect of this method is _repositoriesForAccount is populated with repositories.
    /// If _isSearchingEnabled is true, the path string, and combobox will be populated with values.
    /// </remarks>
    /// <param name="repositoryProvider">The provider.  This should match the display name of the extension</param>
    /// <param name="loginId">The login Id to get the repositories for</param>
    public async Task GetRepositoriesAsync(string repositoryProvider, string loginId)
    {
        var localTask = StartGettingAllRepos(repositoryProvider, loginId);
        await CoordinateTasks(loginId, localTask);
    }

    /// <summary>
    /// Updates the UI with the repositories to display for the specific user and provider.
    /// </summary>
    /// <param name="repositoryProvider">The name of the provider</param>
    /// <param name="loginId">The login ID</param>
    /// <returns>All previously selected repos excluding any added via URL.</returns>
    public IEnumerable<RepoViewListItem> SetRepositories(string repositoryProvider, string loginId)
    {
        RepositoriesToDisplay = new ObservableCollection<RepoViewListItem>(_repositoriesForAccount.Select(x => new RepoViewListItem(x)));

        return _previouslySelectedRepos.Where(x => x.OwningAccount != null)
            .Where(x => x.ProviderName.Equals(repositoryProvider, StringComparison.OrdinalIgnoreCase)
            && x.OwningAccount.LoginId.Equals(loginId, StringComparison.OrdinalIgnoreCase))
            .Select(x => new RepoViewListItem(x.RepositoryToClone));
    }

    /// <summary>
    /// Sets the clone location for all repositories to _cloneLocation
    /// </summary>
    /// <param name="cloneLocation">The location to clone all repositories to.</param>
    public void SetCloneLocation(string cloneLocation)
    {
        _log.Information($"Setting the clone location for all repositories to {cloneLocation}");
        foreach (var cloningInformation in EverythingToClone)
        {
            // N^2 algorithm.  Should change to something else when the number of repos is large.
            if (!_previouslySelectedRepos.Any(x => x == cloningInformation))
            {
                cloningInformation.CloningLocation = new DirectoryInfo(cloneLocation);
            }
        }
    }
}
