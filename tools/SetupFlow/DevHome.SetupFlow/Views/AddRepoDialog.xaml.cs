// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    private readonly Dictionary<string, List<string>> _searchFieldsAndValues;

    /// <summary>
    /// Gets or sets the view model to handle selecting and de-selecting repositories.
    /// </summary>
    public AddRepoViewModel AddRepoViewModel
    {
        get; set;
    }

    public SetupFlowOrchestrator Orchestrator { get; set; }

    /// <summary>
    /// Gets or sets the clone location in case the user decides not to add a dev drive.
    /// </summary>
    public string OldCloneLocation { get; set; }

    public AddRepoDialog(
        SetupFlowOrchestrator setupFlowOrchestrator,
        IDevDriveManager devDriveManager,
        ISetupFlowStringResource stringResource,
        List<CloningInformation> previouslySelectedRepos,
        Guid activityId,
        IHost host)
    {
        this.InitializeComponent();
        _previouslySelectedRepos = previouslySelectedRepos;
        Orchestrator = setupFlowOrchestrator;

        AddRepoViewModel = new AddRepoViewModel(setupFlowOrchestrator, stringResource, previouslySelectedRepos, host, activityId, this, devDriveManager);

        // Changing view to account so the selection changed event for Segment correctly shows URL.
        AddRepoViewModel.CurrentPage = PageKind.AddViaUrl;
        AddRepoViewModel.ShouldEnablePrimaryButton = false;
        AddViaUrlSegmentedItem.IsSelected = true;
        SwitchViewsSegmentedView.SelectedIndex = 1;
        _host = host;
        _searchFieldsAndValues = new();
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
    /// If any items in reposToSelect exist in the UI, select them.
    /// An side-effect of SelectRange is SelectionChanged is fired for each item SelectRange is called on.
    /// IsCallingSelectRange is used to prevent modifying EverythingToClone when repos are being re-selected after filtering.
    /// </summary>
    /// <param name="reposToSelect">The repos to select in the UI.</param>
    public void SelectRepositories(IEnumerable<RepoViewListItem> reposToSelect)
    {
        AddRepoViewModel.IsCallingSelectRange = true;
        var onlyRepoNames = AddRepoViewModel.RepositoriesToDisplay.Select(x => x.RepoName).ToList();
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

        AddRepoViewModel.AddOrRemoveRepository(loginId, e.AddedItems, e.RemovedItems);
        AddRepoViewModel.ToggleCloneButton();
    }

    /// <summary>
    /// The primary button has different behavior based on the screen the user is currently in.
    /// </summary>
    private async void AddRepoContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();

        // Collect search inputs.
        Dictionary<string, string> searchInput = new();
        foreach (var searchBox in ShowingSearchTermsGrid.Children)
        {
            if (searchBox is AutoSuggestBox suggestBox)
            {
                searchInput.Add(suggestBox.Header as string, suggestBox.Text);
            }
        }

        args.Cancel = await AddRepoViewModel.PrimaryButtonClick(searchInput);

        deferral.Complete();
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

    private void FilterSuggestions(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            sender.ItemsSource = _searchFieldsAndValues[sender.Header.ToString()].Where(x => x.Contains(sender.Text));
        }
    }

    private async void SwitchToSearchPage(object sender, RoutedEventArgs e)
    {
        AddRepoViewModel.ChangeToSelectSearchTermsPage();
        _searchFieldsAndValues.Clear();
        ShowingSearchTermsGrid.Children.Clear();
        GatheringSearchValuesGrid.Visibility = Visibility.Visible;
        ShowingSearchTermsGrid.Visibility = Visibility.Collapsed;

        var loginId = (string)AddRepoViewModel.SelectedAccount;
        var searchTerms = AddRepoViewModel.GetSearchTerms();
        ShowingSearchTermsGrid.RowSpacing = 10;

        // Set up the UI for searching.
        var searchTermRow = 0;
        for (var termIndex = 0; termIndex < searchTerms.Count; termIndex++)
        {
            var localTermIndex = termIndex;
            ShowingSearchTermsGrid.RowDefinitions.Add(new RowDefinition());

            var searchFieldName = string.Empty;
            var searchFieldSuggestions = await Task.Run(() => AddRepoViewModel.GetSuggestionsFor(loginId, new(), searchTerms[localTermIndex]));

            _searchFieldsAndValues.Add(searchTerms[localTermIndex], searchFieldSuggestions);
            var suggestBox = new AutoSuggestBox();
            suggestBox.Header = searchTerms[localTermIndex];
            suggestBox.ItemsSource = searchFieldSuggestions;
            suggestBox.Text = searchFieldName;
            suggestBox.TextChanged += FilterSuggestions;
            ShowingSearchTermsGrid.Children.Add(suggestBox);
            Grid.SetRow(suggestBox, searchTermRow++);
        }

        GatheringSearchValuesGrid.Visibility = Visibility.Collapsed;
        ShowingSearchTermsGrid.Visibility = Visibility.Visible;
    }

    public void SetFocusOnSegmentedView()
    {
        SwitchViewsSegmentedView.Focus(FocusState.Programmatic);
    }
}
