// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
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
/// <remarks>
/// AddRepoViewModel stores a reference to this class for the purpos of slowly migreating code from here
/// to the view model.  The refrence the view model has will be removed once the code migration is complete.
/// </remarks>
public partial class AddRepoDialog : ContentDialog
{
    private readonly IHost _host;

    private readonly List<CloningInformation> _previouslySelectedRepos = new();

    /// <summary>
    /// Gets or sets the view model to handle selecting and de-selecting repositories.
    /// </summary>
    public AddRepoViewModel AddRepoViewModel
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

        AddRepoViewModel = new AddRepoViewModel(stringResource, previouslySelectedRepos, host, activityId, this, devDriveManager);

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

    /// <summary>
    /// Sets the event handler on all providers to listen when the user has logged in.
    /// </summary>
    public void SetDeveloperIdChangedEvents()
    {
        AddRepoViewModel.SetChangedEvents(DeveloperIdChangedEventHandler);
    }

    /// <summary>
    /// Changes the flag that indicates if the user is logging in to false to indicate the user is done logging in.
    /// </summary>
    /// <param name="sender">The object that raised this event, should only be IDeveloperId</param>
    /// <param name="developerId">The developer the log in action is applied to.</param>
    public void DeveloperIdChangedEventHandler(object sender, IDeveloperId developerId)
    {
        if (sender is IDeveloperIdProvider devIdProvider)
        {
            var authenticationState = devIdProvider.GetDeveloperIdState(developerId);

            if (authenticationState == AuthenticationState.LoggedIn)
            {
                // AddRepoViewModel uses this to wait for the user to log in before continuing.
                AddRepoViewModel.IsLoggingIn = false;
            }

            // Remove the handler so multiple hooks aren't attached.
            devIdProvider.Changed -= DeveloperIdChangedEventHandler;
        }
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
                location = (AddRepoViewModel.EditDevDriveViewModel.DevDrive != null) ? AddRepoViewModel.EditDevDriveViewModel.GetDriveDisplayName() : string.Empty;
            }

            // In cases where location is empty don't update the cloneLocation. Only update when there are actual values.
            AddRepoViewModel.FolderPickerViewModel.CloneLocation = string.IsNullOrEmpty(location) ? AddRepoViewModel.FolderPickerViewModel.CloneLocation : location;
        }

        AddRepoViewModel.FolderPickerViewModel.ValidateCloneLocation();

        AddRepoViewModel.ToggleCloneButton();
    }

    /// <summary>
    /// If any items in reposToSelect exist in the UI, select them.
    /// An side-effect of SelectRange is SelectionChanged is fired for each item SelectRange is called on.
    /// IsCallingSelectRange is used to prevent modifying EverythingToClone when repos are being re-selected after filtering.
    /// </summary>
    /// <param name="reposToSelect">The repos to select in the UI.</param>
    public void SelectRepositories(IEnumerable<RepoViewListItem> reposToSelect)
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
        var loginId = AddRepoViewModel.SelectedAccount;
        var providerName = (string)RepositoryProviderComboBox.SelectedValue;

        AddRepoViewModel.AddOrRemoveRepository(providerName, loginId, e.AddedItems, e.RemovedItems);
        AddRepoViewModel.ToggleCloneButton();
    }

    /// <summary>
    /// The primary button has different behavior based on the screen the user is currently in.
    /// </summary>
    private async void AddRepoContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (AddRepoViewModel.CurrentPage == PageKind.AddViaUrl)
        {
            // If the user is logging in, the close button text will change.
            // Keep a copy of the original to revert when this button click is done.
            var originalCloseButtonText = AddRepoContentDialog.CloseButtonText;

            // Try to pair a provider with the repo to clone.
            // If the repo is private or does not exist the user will be asked to log in.
            AddRepoViewModel.ShouldShowLoginUi = true;
            AddRepoViewModel.ShouldShowXButtonInLoginUi = true;

            // Get the number of repos already selected to clone in a previous instance.
            // Used to figure out if the repo was added after the user logged into an account.
            var numberOfReposToCloneCount = AddRepoViewModel.EverythingToClone.Count;
            AddRepoViewModel.AddRepositoryViaUri(AddRepoViewModel.Url, AddRepoViewModel.FolderPickerViewModel.CloneLocation);

            // On the first run, ignore any warnings.
            // If this is set to visible and the user needs to log in they'll see an error message after the log-in
            // prompt exits even if they logged in successfully.
            AddRepoViewModel.ShouldShowUrlError = false;

            // Get deferral to prevent the dialog from closing when awaiting operations.
            var deferral = args.GetDeferral();

            // Two click events can not be processed at the same time.
            // UI will not respond to the close button when inside this method.
            // Change the text of the close button to notify users of the X button in the upper-right corner of the log-in ui.
            if (AddRepoViewModel.IsLoggingIn)
            {
                AddRepoContentDialog.CloseButtonText = _host.GetService<ISetupFlowStringResource>().GetLocalized(StringResourceKey.UrlCancelButtonText);
            }

            // Wait roughly 30 seconds for the user to log in.
            var maxIterationsToWait = 30;
            var currentIteration = 0;
            var waitDelay = Convert.ToInt32(new TimeSpan(0, 0, 1).TotalMilliseconds);
            while ((AddRepoViewModel.IsLoggingIn && !AddRepoViewModel.IsCancelling) && currentIteration++ <= maxIterationsToWait)
            {
                await Task.Delay(waitDelay);
            }

            deferral.Complete();
            AddRepoViewModel.ShouldShowLoginUi = false;
            AddRepoViewModel.ShouldShowXButtonInLoginUi = false;

            // User cancelled the login prompt.  Don't re-check repo access.
            if (AddRepoViewModel.IsCancelling)
            {
                return;
            }

            // If the repo was rejected and the user needed to sign in, no repos would've been added
            // and the number of repos to clone will not be changed.
            if (numberOfReposToCloneCount == AddRepoViewModel.EverythingToClone.Count)
            {
                AddRepoViewModel.AddRepositoryViaUri(AddRepoViewModel.Url, AddRepoViewModel.FolderPickerViewModel.CloneLocation);
            }

            if (AddRepoViewModel.ShouldShowUrlError)
            {
                AddRepoViewModel.ShouldEnablePrimaryButton = false;
                args.Cancel = true;
            }

            // Revert the close button text.
            AddRepoContentDialog.CloseButtonText = originalCloseButtonText;
        }
        else if (AddRepoViewModel.CurrentPage == PageKind.AddViaAccount)
        {
            args.Cancel = true;
            var repositoryProviderName = (string)RepositoryProviderComboBox.SelectedItem;
            if (!string.IsNullOrEmpty(repositoryProviderName))
            {
                var deferral = args.GetDeferral();
                await AddRepoViewModel.ChangeToRepoPage();
                deferral.Complete();
            }
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
            AddRepoViewModel.FolderPickerViewModel.CloneLocationAlias = string.Empty;
            AddRepoViewModel.FolderPickerViewModel.InDevDriveScenario = false;
            AddRepoViewModel.EditDevDriveViewModel.RemoveNewDevDrive();
            AddRepoViewModel.FolderPickerViewModel.EnableBrowseButton();
            AddRepoViewModel.FolderPickerViewModel.CloneLocation = _oldCloneLocation;
        }
    }

    /// <summary>
    /// User wants to customize the default dev drive.
    /// </summary>
    private async void CustomizeDevDriveHyperlinkButton_ClickAsync(object sender, RoutedEventArgs e)
    {
        await AddRepoViewModel.EditDevDriveViewModel.PopDevDriveCustomizationAsync();
        AddRepoViewModel.ToggleCloneButton();
    }

    private void RepoUrlTextBox_TextChanged(object sender, RoutedEventArgs e)
    {
        // just in case something other than a text box calls this.
        if (sender is TextBox)
        {
            AddRepoViewModel.Url = (sender as TextBox).Text;
        }

        AddRepoViewModel.ToggleCloneButton();
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
        AddRepoViewModel.EditDevDriveViewModel.MakeDefaultDevDrive();
        AddRepoViewModel.FolderPickerViewModel.DisableBrowseButton();
        _oldCloneLocation = AddRepoViewModel.FolderPickerViewModel.CloneLocation;
        AddRepoViewModel.FolderPickerViewModel.CloneLocation = AddRepoViewModel.EditDevDriveViewModel.GetDriveDisplayName();
        AddRepoViewModel.FolderPickerViewModel.CloneLocationAlias = AddRepoViewModel.EditDevDriveViewModel.GetDriveDisplayName(DevDriveDisplayNameKind.FormattedDriveLabelKind);
        AddRepoViewModel.FolderPickerViewModel.InDevDriveScenario = true;
        AddRepoViewModel.EditDevDriveViewModel.IsDevDriveCheckboxChecked = true;
    }
}
