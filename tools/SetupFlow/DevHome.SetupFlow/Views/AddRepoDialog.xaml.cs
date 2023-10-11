// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.Windows.DevHome.SDK;
using static DevHome.SetupFlow.Models.Common;

namespace DevHome.SetupFlow.Views;

/// <summary>
/// Dialog to allow users to select repositories they want to clone.
/// </summary>
internal partial class AddRepoDialog : ContentDialog
{
    private readonly string _defaultClonePath;

    private readonly IHost _host;

    private readonly List<CloningInformation> _previouslySelectedRepos = new ();

    /// <summary>
    /// Gets or sets the view model to handle selecting and de-selecting repositories.
    /// </summary>
    public AddRepoViewModel AddRepoViewModel
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the view model to handle adding a dev drive.
    /// </summary>
    public EditDevDriveViewModel EditDevDriveViewModel
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the view model to handle the folder picker.
    /// </summary>
    public FolderPickerViewModel FolderPickerViewModel
    {
        get; set;
    }

    /// <summary>
    /// Hold the clone location in case the user decides not to add a dev drive.
    /// </summary>
    private string _oldCloneLocation;

    public AddRepoDialog(
        IDevDriveManager devDriveManager,
        ISetupFlowStringResource stringResource,
        List<CloningInformation> previouslySelectedRepos,
        Guid activityId,
        IHost host)
    {
        this.InitializeComponent();
        _previouslySelectedRepos = previouslySelectedRepos;
        AddRepoViewModel = new AddRepoViewModel(stringResource, previouslySelectedRepos, host, activityId);
        EditDevDriveViewModel = new EditDevDriveViewModel(devDriveManager);
        FolderPickerViewModel = new FolderPickerViewModel(stringResource);

        var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _defaultClonePath = Path.Join(userFolder, "source", "repos");
        FolderPickerViewModel.CloneLocation = _defaultClonePath;

        EditDevDriveViewModel.DevDriveClonePathUpdated += (_, updatedDevDriveRootPath) =>
        {
            FolderPickerViewModel.CloneLocationAlias = EditDevDriveViewModel.GetDriveDisplayName(DevDriveDisplayNameKind.FormattedDriveLabelKind);
            FolderPickerViewModel.CloneLocation = updatedDevDriveRootPath;
        };

        // Changing view to account so the selection changed event for Segment correctly shows URL.
        AddRepoViewModel.CurrentPage = PageKind.AddViaAccount;
        AddRepoViewModel.ShouldEnablePrimaryButton = false;
        AddViaUrlSegmentedItem.IsSelected = true;
        SwitchViewsSegmentedView.SelectedIndex = 1;
        _host = host;
    }

    /// <summary>
    /// Gets all extensions that have a provider type of repository and developerId.
    /// </summary>
    public async Task GetExtensionsAsync()
    {
        await Task.Run(() => AddRepoViewModel.GetExtensions());
    }

    public void SetDeveloperChangedEvents()
    {
        AddRepoViewModel.SetChangedEvents(ChangedEventHandler);
    }

    public void ChangedEventHandler(object sender, IDeveloperId developerId)
    {
        if (sender is IDeveloperIdProvider devIdProvider)
        {
            var authenticationState = devIdProvider.GetDeveloperIdState(developerId);

            if (authenticationState == AuthenticationState.LoggedIn)
            {
                // AddRepoViewModel uses this to wait for the user to log in before continuing.
                AddRepoViewModel.IsLoggingIn = false;
            }

            devIdProvider.Changed -= ChangedEventHandler;
        }
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

    private void ChangeToAccountPage()
    {
        AddRepoViewModel.ChangeToAccountPage();
        FolderPickerViewModel.CloseFolderPicker();
        EditDevDriveViewModel.HideDevDriveUI();

        // If DevHome has 1 provider installed and the provider has 1 logged in account
        // switch to the repo page.
        if (AddRepoViewModel.CanSkipAccountConnection)
        {
            RepositoryProviderComboBox.SelectedValue = AddRepoViewModel.ProviderNames[0];
            SwitchToRepoPage(AddRepoViewModel.ProviderNames[0]);
        }

        SwitchViewsSegmentedView.IsEnabled = true;
        ToggleCloneButton();
    }

    private void ChangeToUrlPage()
    {
        RepositoryProviderComboBox.SelectedIndex = -1;
        AddRepoViewModel.ChangeToUrlPage();
        FolderPickerViewModel.ShowFolderPicker();
        EditDevDriveViewModel.ShowDevDriveUIIfEnabled();
        ToggleCloneButton();
    }

    /// <summary>
    /// Logs the user into the provider if they aren't already.
    /// Changes the page to show all repositories for the user.
    /// </summary>
    /// <remarks>
    /// Fired when the combo box on the account page is changed.
    /// </remarks>
    private void RepositoryProviderNamesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var repositoryProviderName = (string)RepositoryProviderComboBox.SelectedItem;
        if (!string.IsNullOrEmpty(repositoryProviderName))
        {
            PrimaryButtonStyle = AddRepoStackPanel.Resources["ContentDialogLogInButtonStyle"] as Style;
            AddRepoViewModel.ShouldEnablePrimaryButton = true;
        }
        else
        {
            PrimaryButtonStyle = Application.Current.Resources["DefaultButtonStyle"] as Style;
            AddRepoViewModel.ShouldEnablePrimaryButton = false;
        }
    }

    /// <summary>
    /// Open up the folder picker for choosing a clone location.
    /// </summary>
    private async void ChooseCloneLocationButton_Click(object sender, RoutedEventArgs e)
    {
        await FolderPickerViewModel.ChooseCloneLocation();
        ToggleCloneButton();
    }

    /// <summary>
    /// Validate the user put in a rooted, non-null path.
    /// </summary>
    private void CloneLocation_TextChanged(object sender, TextChangedEventArgs e)
    {
        // just in case something other than a text box calls this.
        if (sender is TextBox cloneLocationTextBox)
        {
            var location = cloneLocationTextBox.Text;
            if (string.Equals(cloneLocationTextBox.Name, "DevDriveCloneLocationAliasTextBox", StringComparison.Ordinal))
            {
                location = (EditDevDriveViewModel.DevDrive != null) ? EditDevDriveViewModel.GetDriveDisplayName() : string.Empty;
            }

            // In cases where location is empty don't update the cloneLocation. Only update when there are actual values.
            FolderPickerViewModel.CloneLocation = string.IsNullOrEmpty(location) ? FolderPickerViewModel.CloneLocation : location;
        }

        FolderPickerViewModel.ValidateCloneLocation();

        ToggleCloneButton();
    }

    /// <summary>
    /// Removes all shows repositories from the list view and replaces them with a new set of repositories from a
    /// different account.
    /// </summary>
    /// <remarks>
    /// Fired when a user changes their account on a provider.
    /// </remarks>
    private async void AccountsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // This gets fired when events are removed from the account combo box.
        // When the provider combo box is changed all accounts are removed from the account combo box
        // and new accounts are added. This method fires twice.
        // Once to remove all accounts and once to add all logged in accounts.
        // GetRepositories sets the repositories list view.
        if (e.AddedItems.Count > 0)
        {
            var loginId = (string)AccountsComboBox.SelectedValue;
            var providerName = (string)RepositoryProviderComboBox.SelectedValue;
            await AddRepoViewModel.GetRepositoriesAsync(providerName, loginId);
            SelectRepositories(AddRepoViewModel.SetRepositories(providerName, loginId));
        }
    }

    /// <summary>
    /// If any items in reposToSelect exist in the UI, select them.
    /// An side-effect of SelectRange is SelectionChanged is fired for each item SelectRange is called on.
    /// IsCallingSelectRange is used to prevent modifying EverythingToClone when repos are being re-selected after filtering.
    /// </summary>
    /// <param name="reposToSelect">The repos to select in the UI.</param>
    private void SelectRepositories(IEnumerable<RepoViewListItem> reposToSelect)
    {
        AddRepoViewModel.IsCallingSelectRange = true;
        var onlyRepoNames = AddRepoViewModel.Repositories.Select(x => x.RepoName).ToList();
        foreach (var repoToSelect in reposToSelect)
        {
            var index = onlyRepoNames.IndexOf(repoToSelect.RepoName);
            if (index != -1)
            {
                // SelectRange does not accept an index.  Call it multiple times on each index
                // with a range of 1.
                RepositoriesListView.SelectRange(new ItemIndexRange(index, 1));
            }
        }

        AddRepoViewModel.IsCallingSelectRange = false;
    }

    /// <summary>
    /// If any items in reposToSelect exist in the UI, select them.
    /// An side-effect of SelectRange is SelectionChanged is fired for each item SelectRange is called on.
    /// IsCallingSelectRange is used to prevent modifying EverythingToClone when repos are being re-selected after filtering.
    /// </summary>
    /// <param name="reposToSelect">The repos to select in the UI.</param>
    private void SelectRepositories(IEnumerable<CloningInformation> reposToSelect)
    {
        SelectRepositories(reposToSelect.Select(x => new RepoViewListItem(x.RepositoryToClone)));
    }

    /// <summary>
    /// Adds or removes the selected repository from the list of repos to be cloned.
    /// </summary>
    private void RepositoriesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var loginId = (string)AccountsComboBox.SelectedValue;
        var providerName = (string)RepositoryProviderComboBox.SelectedValue;

        AddRepoViewModel.AddOrRemoveRepository(providerName, loginId, e.AddedItems, e.RemovedItems);
        ToggleCloneButton();
    }

    /// <summary>
    /// Adds the repository from the URL screen to the list of repos to be cloned.
    /// </summary>
    private async void AddRepoContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (AddRepoViewModel.CurrentPage == PageKind.AddViaUrl)
        {
            AddRepoViewModel.IsFetchingRepos = true;
            AddRepoViewModel.AddRepositoryViaUri(AddRepoViewModel.Url, FolderPickerViewModel.CloneLocation, LoginUIContent);

            // On the first run, ignore any warnings.
            // If this is set to visible and the user needs to log in they'll see an error message after the log-in
            // prompt exits even if they logged in successfully.
            AddRepoViewModel.ShouldShowUrlError = Visibility.Collapsed;

            // Get deferral to prevent the dialog from closing when awaiting operations.
            var deferral = args.GetDeferral();

            // Wait 30 seconds for the user to log in.
            var maxIterationsToWait = 30;
            var currentIteration = 0;
            var waitDelay = Convert.ToInt32(new TimeSpan(0, 0, 1).TotalMilliseconds);
            if (AddRepoViewModel.IsLoggingIn && currentIteration <= maxIterationsToWait)
            {
                await Task.Delay(waitDelay);
            }

            deferral.Complete();

            if (!AddRepoViewModel.EverythingToClone.Any())
            {
                AddRepoViewModel.AddRepositoryViaUri(AddRepoViewModel.Url, FolderPickerViewModel.CloneLocation, LoginUIContent);
            }

            if (AddRepoViewModel.ShouldShowUrlError == Visibility.Visible)
            {
                AddRepoViewModel.ShouldEnablePrimaryButton = false;
                args.Cancel = true;
            }

            AddRepoViewModel.IsFetchingRepos = false;
        }
        else if (AddRepoViewModel.CurrentPage == PageKind.AddViaAccount)
        {
            args.Cancel = true;
            var repositoryProviderName = (string)RepositoryProviderComboBox.SelectedItem;
            if (!string.IsNullOrEmpty(repositoryProviderName))
            {
                SwitchToRepoPage(repositoryProviderName);
            }
        }
    }

    private async void SwitchToRepoPage(string repositoryProviderName)
    {
        await AddRepoViewModel.GetAccountsAsync(repositoryProviderName, LoginUIContent);
        if (AddRepoViewModel.Accounts.Any())
        {
            AddRepoViewModel.ChangeToRepoPage();
            FolderPickerViewModel.ShowFolderPicker();
            EditDevDriveViewModel.ShowDevDriveUIIfEnabled();
            AccountsComboBox.SelectedValue = AddRepoViewModel.Accounts.First();
            AddRepoViewModel.ShouldEnablePrimaryButton = false;
        }
    }

    /// <summary>
    /// Adds or removes the default dev drive.  This dev drive will be made at the loading screen.
    /// </summary>
    private void MakeNewDevDriveCheckBox_Click(object sender, RoutedEventArgs e)
    {
        // Getting here means
        // 1. The user does not have any existing dev drives
        // 2. The user wants to clone to a new dev drive.
        // 3. The user un-checked this and does not want a new dev drive.
        var isChecked = (sender as CheckBox).IsChecked;
        if (isChecked.Value)
        {
            UpdateDevDriveInfo();
        }
        else
        {
            FolderPickerViewModel.CloneLocationAlias = string.Empty;
            FolderPickerViewModel.InDevDriveScenario = false;
            EditDevDriveViewModel.RemoveNewDevDrive();
            FolderPickerViewModel.EnableBrowseButton();
            FolderPickerViewModel.CloneLocation = _oldCloneLocation;
        }
    }

    /// <summary>
    /// User wants to customize the default dev drive.
    /// </summary>
    private async void CustomizeDevDriveHyperlinkButton_ClickAsync(object sender, RoutedEventArgs e)
    {
        await EditDevDriveViewModel.PopDevDriveCustomizationAsync();
        ToggleCloneButton();
    }

    /// <summary>
    /// Toggles the clone button.  Make sure other view models have correct information.
    /// </summary>
    private void ToggleCloneButton()
    {
        var isEverythingGood = AddRepoViewModel.ValidateRepoInformation() && FolderPickerViewModel.ValidateCloneLocation();
        if (EditDevDriveViewModel.DevDrive != null && EditDevDriveViewModel.DevDrive.State != DevDriveState.ExistsOnSystem)
        {
            isEverythingGood &= EditDevDriveViewModel.IsDevDriveValid();
        }

        if (isEverythingGood)
        {
            AddRepoViewModel.ShouldEnablePrimaryButton = true;
        }
        else
        {
            AddRepoViewModel.ShouldEnablePrimaryButton = false;
        }

        // Fill in EverythingToClone with the location
        if (isEverythingGood)
        {
            AddRepoViewModel.SetCloneLocation(FolderPickerViewModel.CloneLocation);
        }
    }

    private void RepoUrlTextBox_TextChanged(object sender, RoutedEventArgs e)
    {
        // just in case something other than a text box calls this.
        if (sender is TextBox)
        {
            AddRepoViewModel.Url = (sender as TextBox).Text;
        }

        ToggleCloneButton();
    }

    private void Segmented_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AddRepoViewModel.CurrentPage == PageKind.AddViaUrl)
        {
            ChangeToAccountPage();
        }
        else
        {
            ChangeToUrlPage();
        }
    }

    /// <summary>
    /// Putting the event in the view so SelectRange can be called.
    /// SelectRange needs a reference to the ListView.
    /// </summary>
    /// <param name="sender">Who fired the event</param>
    /// <param name="e">Any args</param>
    private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Just in case something other than a text box calls this.
        if (sender is TextBox)
        {
            AddRepoViewModel.FilterRepositories(FilterTextBox.Text);
            SelectRepositories(AddRepoViewModel.EverythingToClone);
        }
    }

    /// <summary>
    /// Update dialog to show Dev Drive information.
    /// </summary>
    public void UpdateDevDriveInfo()
    {
        EditDevDriveViewModel.MakeDefaultDevDrive();
        FolderPickerViewModel.DisableBrowseButton();
        _oldCloneLocation = FolderPickerViewModel.CloneLocation;
        FolderPickerViewModel.CloneLocation = EditDevDriveViewModel.GetDriveDisplayName();
        FolderPickerViewModel.CloneLocationAlias = EditDevDriveViewModel.GetDriveDisplayName(DevDriveDisplayNameKind.FormattedDriveLabelKind);
        FolderPickerViewModel.InDevDriveScenario = true;
        EditDevDriveViewModel.IsDevDriveCheckboxChecked = true;
    }
}
