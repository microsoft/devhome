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
using CommunityToolkit.WinUI.Collections;
using CommunityToolkit.WinUI.Controls;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.DeveloperId;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.Contracts.Services;
using DevHome.Logging;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.Views;
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
    [NotifyPropertyChangedFor(nameof(IsAccountComboBoxEnabled))]
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
    private IncrementalLoadingCollection<IncrementalRepoViewItemViewModel, RepoViewListItem> _repositories = new();

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
    /// Should the error text be shown?
    /// </summary>
    [ObservableProperty]
    private bool _showErrorTextBox;

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
    private bool _shouldShowUrlError;

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
    /// Used to figure out what button is pressed for the split button.
    /// This determines the UI elements shown/hidden.
    /// </summary>
    private enum SegmentedItemTag
    {
        Account,
        URL,
    }

    /// <summary>
    /// Compares against the tags in the sort order combo box.
    /// Determines how to sort the repos.
    /// </summary>
    private enum SortMethod
    {
        NameAscending,
        NameDescending,
    }

    /// <summary>
    /// Hides/Shows UI elements for the selected button.
    /// </summary>
    /// <param name="selectedItem">The button the user clicked on.</param>
    [RelayCommand]
    public void ChangePage(SegmentedItem selectedItem)
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
            ChangeToAccountPage();
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
    /// Filters all repos down to any that start with text.
    /// A side-effect of filtering is that SelectionChanged fires for every selected repo but only on removal.
    /// SelectionChanged isn't fired for re-adding because repos are removed, not added.  To prevent the RepoTool from forgetting the repos that were selected
    /// the flag _isFiltering is used to prevent modifications to EverythingToClone.
    /// Once filtering is done SelectRange is called on each item in EverythingToClone to re-select them.
    /// </summary>
    /// <param name="text">The text to use with .Contains</param>
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
                .Where(x => x.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        _isFiltering = true;
        var localRepositoires = OrderRepos(filteredRepositories).ToList();
        var indexer = new IncrementalRepoViewItemViewModel(localRepositoires);
        Repositories = new IncrementalLoadingCollection<IncrementalRepoViewItemViewModel, RepoViewListItem>(indexer);
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
        var organizationRepos = repos.Where(x => !x.OwningAccountName.Equals(SelectedAccount, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.LastUpdated)
            .Select(x => new RepoViewListItem(x));

        var userRepos = repos.Where(x => x.OwningAccountName.Equals(SelectedAccount, StringComparison.OrdinalIgnoreCase));
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
    /// Switches the repos shown to the account selected.
    /// </summary>
    [RelayCommand]
    private void MenuItemClick(string selectedItemName)
    {
        _host.GetService<WindowEx>().DispatcherQueue.TryEnqueue(async () =>
        {
            SelectedAccount = selectedItemName;
            await GetRepositoriesAsync(_selectedRepoProvider, SelectedAccount);

            var sdkDisplayName = _providers.GetSDKProvider(_selectedRepoProvider).DisplayName;
            _addRepoDialog.SelectRepositories(SetRepositories(sdkDisplayName, SelectedAccount));
        });
    }

    [RelayCommand]
    public void SortRepos(TextBlock selectedItem)
    {
        if (selectedItem.Tag == null)
        {
            return;
        }

        IEnumerable<IRepository> repositories = new List<IRepository>();
        if (!Enum.TryParse<SortMethod>(selectedItem.Tag.ToString(), out var sortMethod))
        {
            return;
        }

        if (sortMethod.Equals(SortMethod.NameAscending))
        {
            repositories = _repositoriesForAccount
                .OrderBy(x => Path.Join(x.OwningAccountName, x.DisplayName));
        }

        if (sortMethod.Equals(SortMethod.NameDescending))
        {
            repositories = _repositoriesForAccount
                .OrderByDescending(x => Path.Join(x.OwningAccountName, x.DisplayName));
        }

        var reposToShow = repositories.Select(x => new RepoViewListItem(x)).ToList();

        var indexer = new IncrementalRepoViewItemViewModel(reposToShow);
        Repositories = new IncrementalLoadingCollection<IncrementalRepoViewItemViewModel, RepoViewListItem>(indexer);
    }

    [RelayCommand]
    private async Task OpenFolderPicker()
    {
        await FolderPickerViewModel.ChooseCloneLocation();
        ToggleCloneButton();
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
        AccountsToShow = new MenuFlyout();
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
                Log.Logger?.ReportError(Log.Component.RepoConfig, $"{newAccount.Count()} accounts logged in at once.  Choosing the first alphabetically");
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

    public AddRepoViewModel(
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

        _previouslySelectedRepos = previouslySelectedRepos ?? new List<CloningInformation>();
        EverythingToClone = new List<CloningInformation>(_previouslySelectedRepos);
        _activityId = activityId;
        FolderPickerViewModel = new FolderPickerViewModel(stringResource);

        var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var defaultClonePath = Path.Join(userFolder, "source", "repos");
        FolderPickerViewModel.CloneLocation = defaultClonePath;

        EditDevDriveViewModel = new EditDevDriveViewModel(devDriveManager);

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
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Getting installed extensions with Repository and DevId providers");
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
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Changing to Url page");
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

    public void ChangeToAccountPage()
    {
        AccountIndex = -1;

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

        FolderPickerViewModel.CloseFolderPicker();
        EditDevDriveViewModel.HideDevDriveUI();

        // If DevHome has 1 provider installed and the provider has 1 logged in account
        // switch to the repo page.
        if (CanSkipAccountConnection)
        {
            ChangeToRepoPage().Wait();
            return;
        }

        Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Changing to Account page");
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

    public async Task ChangeToRepoPage()
    {
        await GetAccountsAsync(_selectedRepoProvider, LoginUiContent);
        if (Accounts.Any())
        {
            FolderPickerViewModel.ShowFolderPicker();
            EditDevDriveViewModel.ShowDevDriveUIIfEnabled();
            SelectedAccount = Accounts.First();
            ShouldEnablePrimaryButton = false;
        }

        Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Changing to Repo page");
        ShowUrlPage = false;
        ShowAccountPage = false;
        ShowRepoPage = true;
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

        // The dialog makes a user log in if they have no accounts.
        // keep this here just in case.
        if (Accounts.Any())
        {
            SelectedAccount = Accounts.First();
            MenuItemClick((AccountsToShow.Items[0] as MenuFlyoutItem).Text);
        }
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
                Log.Logger?.ReportError(Log.Component.RepoConfig, $"Invalid URL {uri.OriginalString}", e);
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
        var provider = _providers.CanAnyProviderSupportThisUri(uri);

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
            Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Repository has already been added.");
            TelemetryFactory.Get<ITelemetry>().LogCritical("RepoTool_RepoAlreadyAdded_Event", false, _activityId);
            return;
        }

        Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Adding repository to clone {cloningInformation.RepositoryId} to location '{cloneLocation}'");

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

            InitiateAddAccountUserExperienceAsync(provider, loginFrame);
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
        InitiateAddAccountUserExperienceAsync(provider, loginFrame);
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
        SelectedAccount = loginId;
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
        var localRepositoires = OrderRepos(_repositoriesForAccount).ToList();
        var indexer = new IncrementalRepoViewItemViewModel(localRepositoires);
        Repositories = new IncrementalLoadingCollection<IncrementalRepoViewItemViewModel, RepoViewListItem>(indexer);

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
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Setting the clone location for all repositories to {cloneLocation}");
        foreach (var cloningInformation in EverythingToClone)
        {
            // N^2 algorithm.  Shouldn't be too slow unless at least 100 repos are added.
            if (!_previouslySelectedRepos.Any(x => x == cloningInformation))
            {
                cloningInformation.CloningLocation = new DirectoryInfo(cloneLocation);
            }
        }
    }
}
