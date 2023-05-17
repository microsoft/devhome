// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Configuration.Provider;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Windows.Security.Authentication.Identity.Provider;
using static DevHome.SetupFlow.Models.Common;

namespace DevHome.SetupFlow.Views;

/// <summary>
/// Dialog to allow users to select repositories they want to clone.
/// </summary>
internal partial class AddRepoDialog
{
    private readonly string _defaultClonePath;

    private readonly List<CloningInformation> _previouslySelectedRepos = new ();

    /// <summary>
    /// Gets or sets the view model to handle selecting and de-selecting repositories.
    /// </summary>
    public AddRepoViewModel AddRepoViewModel
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the view model to handle added a dev drive.
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

    public AddRepoDialog(IDevDriveManager devDriveManager, ISetupFlowStringResource stringResource, List<CloningInformation> previouslySelectedRepos)
    {
        this.InitializeComponent();
        _previouslySelectedRepos = previouslySelectedRepos;
        AddRepoViewModel = new AddRepoViewModel(stringResource, previouslySelectedRepos);
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
        IsPrimaryButtonEnabled = false;
        AddViaUrlSegmentedItem.IsSelected = true;
        SwitchViewsSegmentedView.SelectedIndex = 1;
    }

    /// <summary>
    /// Gets all plugins that have a provider type of repository and devid.
    /// </summary>
    public async Task GetPluginsAsync()
    {
        await Task.Run(() => AddRepoViewModel.GetPlugins());
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
        SwitchViewsSegmentedView.IsEnabled = false;
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
            IsPrimaryButtonEnabled = true;
        }
        else
        {
            PrimaryButtonStyle = Application.Current.Resources["DefaultButtonStyle"] as Style;
            IsPrimaryButtonEnabled = false;
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
    /// Validate the user put in an absolute path when they are done typing.
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

            FolderPickerViewModel.CloneLocation = location;
        }

        FolderPickerViewModel.ValidateCloneLocation();

        ToggleCloneButton();
    }

    /// <summary>
    /// Removes all shows repositories from the list view and replaces them with a new set of repositories from a
    /// diffrent account.
    /// </summary>
    /// <remarks>
    /// Fired when a user changes their account on a provider.
    /// </remarks>
    private void AccountsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // This gets fired when events are removed from the account combo box.
        // When the provider combox is changed all accounts are removed from the account combo box
        // and new accounts are added. This method fires twice.
        // Once to remove all accounts and once to add all logged in accounts.
        // GetRepositories sets the repositories list view.
        if (e.AddedItems.Count > 0)
        {
            var loginId = (string)AccountsComboBox.SelectedValue;
            var providerName = (string)RepositoryProviderComboBox.SelectedValue;
            SelectRepositories(AddRepoViewModel.GetRepositories(providerName, loginId));
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
                // Seems like the "Correct" way to pre-select items in a list view is to call range.
                // SelectRange does not accept an index though.  Call it multiple times on each index
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
    private void AddRepoContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (AddRepoViewModel.CurrentPage == PageKind.AddViaUrl)
        {
            AddRepoViewModel.AddRepositoryViaUri(AddRepoViewModel.Url, FolderPickerViewModel.CloneLocation);
            if (AddRepoViewModel.ShouldShowUrlError == Visibility.Visible)
            {
                IsPrimaryButtonEnabled = false;
                args.Cancel = true;
            }
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
        var getAccountsTask = AddRepoViewModel.GetAccountsAsync(repositoryProviderName);
        AddRepoViewModel.ChangeToRepoPage();
        FolderPickerViewModel.ShowFolderPicker();
        EditDevDriveViewModel.ShowDevDriveUIIfEnabled();

        await getAccountsTask;
        if (AddRepoViewModel.Accounts.Any())
        {
            AccountsComboBox.SelectedValue = AddRepoViewModel.Accounts.First();
        }

        PrimaryButtonStyle = Application.Current.Resources["DefaultButtonStyle"] as Style;
        IsPrimaryButtonEnabled = false;
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
            EditDevDriveViewModel.MakeDefaultDevDrive();
            FolderPickerViewModel.DisableBrowseButton();
            _oldCloneLocation = FolderPickerViewModel.CloneLocation;
            FolderPickerViewModel.CloneLocation = EditDevDriveViewModel.GetDriveDisplayName();
            FolderPickerViewModel.CloneLocationAlias = EditDevDriveViewModel.GetDriveDisplayName(DevDriveDisplayNameKind.FormattedDriveLabelKind);
            FolderPickerViewModel.InDevDriveScenario = true;
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
            IsPrimaryButtonEnabled = true;
        }
        else
        {
            IsPrimaryButtonEnabled = false;
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
}
