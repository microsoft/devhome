// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.RepoConfig.Models;
using DevHome.SetupFlow.RepoConfig.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinUIEx;
using static DevHome.SetupFlow.RepoConfig.Models.Common;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace DevHome.SetupFlow.RepoConfig.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class AddRepoDialog
{
    // Used in a drop down for the user to select what account to log into.
    // Currently 1 account per repository provider.
    private readonly ObservableCollection<string> repositoryProviderNamesToShow = new ();

    // Used to keep track of relevant information to make cloning possible.
    private readonly CloningInformation cloningInformation;

    private readonly AddRepoViewModel addRepoViewModel = new ();

    // In the repositories page the user can change between repositories by
    // changing the account they are looking at.
    private ObservableCollection<string> _loginIdsToShow = new ();

    // Holds all dev volume location.
    private List<string> devVolumeLocations;

    private List<IDeveloperId> loggedInAccounts = new ();

    private List<IRepository> avalibleRepositoresToSelectFrom = new ();

    // Collection of UserName/RepositoryName
    private ObservableCollection<string> repositoriesToShow = new ();

    public AddRepoDialog(CloningInformation cloningInformation)
    {
        this.cloningInformation = cloningInformation;

        this.InitializeComponent();

        cloningInformation.CurrentPage = CurrentPage.AddViaUrl;
        cloningInformation.CloneLocationSelectionMethod = CloneLocationSelectionMethod.LocalPath;
    }

    public async Task StartPlugins()
    {
        await addRepoViewModel.StartPlugins();

        foreach (var providerName in addRepoViewModel.QueryForAllProviderNames())
        {
            repositoryProviderNamesToShow.Add(providerName);
        }

        devVolumeLocations = addRepoViewModel.QueryForNewAndExistingDevVolumes();

        DevVolumeComboBox.IsEnabled = devVolumeLocations.Any();
    }

    /// <summary>
    /// Clicking away from URL
    /// Can't click on account now only URL.
    /// Hide URL UI.
    /// Show Account UI.
    /// </summary>
    private void AddViaAccountToggleButton_Click(object sender, RoutedEventArgs e)
    {
        AddViaUrlToggleButton.IsChecked = false;
        TogglePageSwitch(CurrentPage.AddViaAccount);
        ToggleCloneButton();
    }

    /// <summary>
    /// Clicking away from Account
    /// Can't click on URL.  Only account
    /// Show URL UI
    /// Hide Account UI
    /// </summary>
    private void AddViaUrlToggleButton_Click(object sender, RoutedEventArgs e)
    {
        AddViaAccountToggleButton.IsChecked = false;
        TogglePageSwitch(CurrentPage.AddViaUrl);
        ToggleCloneButton();
    }

    /// <summary>
    ///   Opens the directory picker and saves the location if a location was chosen.
    /// </summary>
    private async void ChooseCloneLocationButton_Click(object sender, RoutedEventArgs e)
    {
        var maybeCloneLocation = await addRepoViewModel.PickCloneDirectoryAsync();

        if (maybeCloneLocation != null)
        {
            // Save the location to both URL and Account text boxes so the user
            // can easily switch betweeen them.
            CloneLocationForUrlTextBox.Text = maybeCloneLocation.FullName;
            CloneLocationForAccountTextBox.Text = maybeCloneLocation.FullName;

            cloningInformation.CloneLocation = new DirectoryInfo(maybeCloneLocation.FullName);
        }

        ToggleCloneButton();
    }

    /// <summary>
    /// Toggles the clone button.  Different pages have different logic to figure this out.
    /// </summary>
    private void ToggleCloneButton()
    {
        // Different pages have different input fields and different validation.
        // Different pages save different information.
        if (cloningInformation.CurrentPage == CurrentPage.AddViaUrl)
        {
            // Check if the user entered a url or username/repository combination, and
            // the user has selected a clone location.
            IsPrimaryButtonEnabled = (cloningInformation.UrlOrUsernameRepo.Length > 0) &&
                (cloningInformation.CloneLocation != null && cloningInformation.CloneLocation.FullName.Length > 0);
        }
        else if (cloningInformation.CurrentPage == CurrentPage.AddViaAccount || cloningInformation.CurrentPage == CurrentPage.Repositories)
        {
            // User has to go through the account dialog before selecting repositories.
            // They have the same validation because the repositories selected to clone are saved if the user
            // clicks into the URL dialog.
            IsPrimaryButtonEnabled = (cloningInformation.RepositoriesToClone.Count > 0) &&
                cloningInformation.CloneLocation != null && cloningInformation.CloneLocation.FullName.Length > 0;
        }
    }

    /// <summary>
    ///  switches between the three grids.
    /// </summary>
    /// <param name="pageNavigatingTo">The page the user is navigating to.  Allows us to figure out what to collapse</param>
    private void TogglePageSwitch(CurrentPage pageNavigatingTo)
    {
        // Clicking away from Account or Repositories
        if (pageNavigatingTo == CurrentPage.AddViaUrl)
        {
            AddUrlGrid.Visibility = Visibility.Visible;
            AddAccountGrid.Visibility = Visibility.Collapsed;
            SelectRepositoriesGrid.Visibility = Visibility.Collapsed;
        }
        else if (pageNavigatingTo == CurrentPage.AddViaAccount)
        {
            // Clicking away from URL
            AddUrlGrid.Visibility = Visibility.Collapsed;
            AddAccountGrid.Visibility = Visibility.Visible;
        }
        else if (pageNavigatingTo == CurrentPage.Repositories)
        {
            // User has switched to an account.  Show the information.
            // Only way to get to this page is via accounts.
            AddAccountGrid.Visibility = Visibility.Collapsed;
            SelectRepositoriesGrid.Visibility = Visibility.Visible;
            ClonePathComboBox.SelectedIndex = 0;
        }

        cloningInformation.CurrentPage = pageNavigatingTo;
    }

    /// <summary>
    /// Saves the directory cloning location.
    /// </summary>
    private void RepoUrlOrNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var repoUrlOrName = RepoUrlOrNameTextBox.Text;
        if (!string.IsNullOrEmpty(repoUrlOrName))
        {
            cloningInformation.UrlOrUsernameRepo = RepoUrlOrNameTextBox.Text;
        }

        ToggleCloneButton();
    }

    /// <summary>
    /// Logs the user into the provider if they aren't already.  Then set up the display names for the repositories grid.
    /// </summary>
    private void RepositoryProviderNamesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var repositoryProviderName = (string)RepositoryProviderNamesComboBox.SelectedItem;
        loggedInAccounts = addRepoViewModel.GetAllLoggedInAccounts(repositoryProviderName).ToList();

        if (!loggedInAccounts.Any())
        {
            addRepoViewModel.LogIntoProvider(repositoryProviderName);

            // Logging in opens a webpage.
            // Wait a few seconds for authorization to finish.
            Thread.Sleep(5000);
            loggedInAccounts = addRepoViewModel.GetAllLoggedInAccounts(repositoryProviderName).ToList();
        }

        _loginIdsToShow = new ObservableCollection<string>(loggedInAccounts.Select(developerId => developerId.LoginId()));

        LoginIdsComboBox.ItemsSource = _loginIdsToShow;
        LoginIdsComboBox.SelectedIndex = 0;

        TogglePageSwitch(CurrentPage.Repositories);
    }

    /// <summary>
    /// Users have 2 options for choosing a location.  Full path or dev volume.  This switches between the two grids.
    /// </summary>
    private void ToggleCloneLocationGrids()
    {
        if (cloningInformation.CloneLocationSelectionMethod == CloneLocationSelectionMethod.LocalPath)
        {
            CloneLocationForAccountTextBox.Visibility = Visibility.Visible;
            ChooseCloneLocationForAccountButton.Visibility = Visibility.Visible;

            DevVolumeComboBox.Visibility = Visibility.Collapsed;
        }
        else if (cloningInformation.CloneLocationSelectionMethod == CloneLocationSelectionMethod.DevVolume)
        {
            CloneLocationForAccountTextBox.Visibility = Visibility.Collapsed;
            ChooseCloneLocationForAccountButton.Visibility = Visibility.Collapsed;

            DevVolumeComboBox.Visibility = Visibility.Visible;
        }
    }

    /// <summary>
    /// Saves how the user wants to pick their cloning location.
    /// </summary>
    private void ClonePathComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // User can choose between Local Path and dev volume.
        var pathChoice = (string)e.AddedItems[0];
        if (pathChoice.Equals("Local Path", StringComparison.OrdinalIgnoreCase))
        {
            cloningInformation.CloneLocationSelectionMethod = CloneLocationSelectionMethod.LocalPath;
        }
        else
        {
            cloningInformation.CloneLocationSelectionMethod = CloneLocationSelectionMethod.DevVolume;
        }

        ToggleCloneLocationGrids();
    }

    /// <summary>
    /// Removes all shows repositories from the list view and replaces them with a new set of repositories from a
    /// diffrent account.
    /// </summary>
    private async void LoginIdsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Switching account.  Change Repositories.
        repositoriesToShow.Clear();
        avalibleRepositoresToSelectFrom.Clear();

        var loginId = (string)LoginIdsComboBox.SelectedValue;
        var developerId = loggedInAccounts.FirstOrDefault(x => x.LoginId().Equals(loginId, StringComparison.OrdinalIgnoreCase));

        var repositoryProviderName = (string)RepositoryProviderNamesComboBox.SelectedItem;
        var repositories = await addRepoViewModel.GetRepositoriesAsync(repositoryProviderName, developerId);

        if (repositories != null)
        {
            foreach (var repository in repositories)
            {
                repositoriesToShow.Add(repository.DisplayName());
            }

            avalibleRepositoresToSelectFrom = repositories.ToList();
        }
    }

    /// <summary>
    /// Gets the repository to be cloned and either adds or removes it from the list.
    /// </summary>
    private void RepositoriesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var loginId = (string)LoginIdsComboBox.SelectedValue;

        var idForRepos = loggedInAccounts.FirstOrDefault(x => x.LoginId() == loginId);
        var selectedItems = new List<object>(e.AddedItems);

        // removed and added items go through the same code path.
        // Add all removed items to one list.
        selectedItems.AddRange(new List<object>(e.RemovedItems));

        foreach (var selectedItem in selectedItems)
        {
            var repositoryToAddOrRemove = avalibleRepositoresToSelectFrom.FirstOrDefault(x => x.DisplayName().Equals(selectedItem as string, StringComparison.OrdinalIgnoreCase));

            if (repositoryToAddOrRemove != null)
            {
                cloningInformation.AddRepositoryOrRemoveIfExists(idForRepos, repositoryToAddOrRemove);
            }
        }

        ToggleCloneButton();
    }

    /// <summary>
    /// Writs the cloning location to all text boxes that show a clone location.
    /// </summary>
    private void CloneLocationForUrlTextBox_TextChanged(object sender, RoutedEventArgs e)
    {
        var locationToCloneTo = string.Empty;
        if (cloningInformation.CurrentPage == CurrentPage.AddViaUrl)
        {
            locationToCloneTo = CloneLocationForUrlTextBox.Text;
        }
        else if (cloningInformation.CurrentPage == CurrentPage.AddViaAccount || cloningInformation.CurrentPage == CurrentPage.Repositories)
        {
            locationToCloneTo = CloneLocationForAccountTextBox.Text;
        }

        // The control could lose focus when nothing was typed into the text box.
        if (string.IsNullOrEmpty(locationToCloneTo) || string.IsNullOrWhiteSpace(locationToCloneTo))
        {
            cloningInformation.CloneLocation = null;
            ToggleCloneButton();
            return;
        }

        // Did the user put in an absolute path?
        if (Path.IsPathRooted(locationToCloneTo))
        {
            cloningInformation.CloneLocation = new DirectoryInfo(locationToCloneTo);
        }
        else
        {
            // ApplicationData isn't the best option. This can be discussed.
            var fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), locationToCloneTo);
            cloningInformation.CloneLocation = new DirectoryInfo(fullPath);
        }

        // Modify the path in both URL and account so the user does not have to re-enter details.
        // If the user puts in a relative path, should we replace the text with the full path so the user can see where
        // the repos will be cloned?
        CloneLocationForUrlTextBox.Text = locationToCloneTo;
        CloneLocationForAccountTextBox.Text = locationToCloneTo;

        ToggleCloneButton();
    }
}
