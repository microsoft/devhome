// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Services;
using DevHome.Common.Windows.FileDialog;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.Database.Services;
using DevHome.RepositoryManagement.Factories;
using DevHome.RepositoryManagement.Models;
using DevHome.RepositoryManagement.Services;
using DevHome.SetupFlow.Common.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Windows.Storage;

namespace DevHome.RepositoryManagement.ViewModels;

public partial class RepositoryManagementMainPageViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementMainPageViewModel));

    private static readonly string[] _commitProperties =
    [
        "System.VersionControl.LastChangeAuthorName",
        "System.VersionControl.LastChangeDate",
        "System.VersionControl.LastChangeID",
    ];

    private readonly INavigationService _navigationService;

    private readonly RepositoryManagementItemViewModelFactory _factory;

    private readonly RepositoryManagementDataAccessService _dataAccessService;

    private readonly StringResource _stringResource = new("DevHome.RepositoryManagement.pri", "DevHome.RepositoryManagement/Resources");

    private readonly RepositoryEnhancerService _enhanceRepositoryService;

    private readonly Window _window;

    private List<RepositoryManagementItemViewModel> _allLineItems = [];

    private List<Repository> _allRepositoriesFromTheDatabase;

    [ObservableProperty]
    private ObservableCollection<RepositoryManagementItemViewModel> _lineItemsToDisplay;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private bool _areFilterAndSortEnabled;

    public enum SortOrder
    {
        NameAscending,
        NameDescending,
    }

    private SortOrder _sortTag = SortOrder.NameAscending;

    [RelayCommand]
    public void ChangeSortOrder(ComboBoxItem selectedItem)
    {
        if (selectedItem == null)
        {
            _log.Warning($"selectedItem in {nameof(ChangeSortOrder)} is null.  Not changing order.");
            return;
        }

        if (selectedItem.Tag == null)
        {
            _log.Warning($"selectedItem.Tag in {nameof(ChangeSortOrder)} is null.  Not changing order.");
            return;
        }

        var sortOrder = selectedItem.Tag.ToString();
        if (sortOrder.Equals(SortOrder.NameAscending.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            _sortTag = SortOrder.NameAscending;
        }
        else
        {
            _sortTag = SortOrder.NameDescending;
        }

        AreFilterAndSortEnabled = false;

        UpdateDisplayedRepositories();

        AreFilterAndSortEnabled = true;
    }

    [RelayCommand]
    public void FilterRepositories()
    {
        UpdateDisplayedRepositories();
    }

    [RelayCommand]
    public async Task AddExistingRepository()
    {
        AreFilterAndSortEnabled = false;

        var existingRepositoryLocation = await GetRepositoryLocationFromUser();
        if (existingRepositoryLocation.Equals(string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            _log.Warning($"Repository in {nameof(AddExistingRepository)} is either empty.");
            return;
        }

        var sourceControlForThisRepository = await _enhanceRepositoryService.MakeRepositoryEnhanced(existingRepositoryLocation);
        if (sourceControlForThisRepository == Guid.Empty)
        {
            _log.Warning($"A Source Control extension could not be determined for the repository at {existingRepositoryLocation}.");
        }

        var repositoryUrl = _enhanceRepositoryService.GetRepositoryUrl(existingRepositoryLocation);
        var configurationFileLocation = DscHelpers.GetConfigurationFileIfExists(existingRepositoryLocation);
        var newRepository = _dataAccessService.MakeRepository(
            Path.GetFileName(existingRepositoryLocation),
            existingRepositoryLocation,
            configurationFileLocation,
            repositoryUrl,
            sourceControlForThisRepository);

        _allRepositoriesFromTheDatabase.Add(newRepository);

        var repositoryWithCommit = GetRepositoryAndLatestCommitPairs(new List<Repository> { newRepository });
        var newLineItem = ConvertToLineItems(repositoryWithCommit);

        if (newLineItem.Count > 0)
        {
            _allLineItems.Add(newLineItem[0]);
        }
        else
        {
            _log.Warning("A new line item was not made.");
        }

        UpdateDisplayedRepositories();

        AreFilterAndSortEnabled = true;
    }

    [RelayCommand]
    public void NavigateToCloneRepositoryExpirence()
    {
        _navigationService.NavigateTo(KnownPageKeys.SetupFlow, KnownPageKeys.RepositoryConfiguration);
    }

    [RelayCommand]
    public async Task LoadRepositories()
    {
        // TODO: Spinning progress ring when loading repositories.
        AreFilterAndSortEnabled = false;
        _allRepositoriesFromTheDatabase = _dataAccessService.GetRepositories();
        _allRepositoriesFromTheDatabase = await AssignSourceControlId(_allRepositoriesFromTheDatabase);

        var repositoriesWithCommits = GetRepositoryAndLatestCommitPairs(_allRepositoriesFromTheDatabase);

        _allLineItems.Clear();
        LineItemsToDisplay.Clear();

        _allLineItems = ConvertToLineItems(repositoriesWithCommits);
        LineItemsToDisplay = new(HideFilterAndSort(_allLineItems).Where(x => x.IsHiddenFromPage == false).ToList());

        AreFilterAndSortEnabled = true;
    }

    [RelayCommand]
    public void HideRepository(RepositoryManagementItemViewModel repository)
    {
        if (repository == null)
        {
            return;
        }

        AreFilterAndSortEnabled = false;

        repository.RemoveThisRepositoryFromTheList();
        repository.IsHiddenFromPage = true;
        UpdateDisplayedRepositories();

        AreFilterAndSortEnabled = true;
    }

    public RepositoryManagementMainPageViewModel(
        RepositoryManagementItemViewModelFactory factory,
        RepositoryManagementDataAccessService dataAccessService,
        INavigationService navigationService,
        RepositoryEnhancerService enchanceRepositoryService,
        Window window)
    {
        _dataAccessService = dataAccessService;
        _factory = factory;
        LineItemsToDisplay = [];
        _navigationService = navigationService;
        _enhanceRepositoryService = enchanceRepositoryService;
        _window = window;
    }

    private void UpdateDisplayedRepositories()
    {
        LineItemsToDisplay.Clear();
        var lineItemsToShow = HideFilterAndSort(_allLineItems);
        lineItemsToShow.ForEach(x => LineItemsToDisplay.Add(x));
    }

    private List<RepositoryManagementItemViewModel> ConvertToLineItems(List<(Repository, Commit)> repositories)
    {
        _log.Information("Converting repositories from the database into view models for display");
        List<RepositoryManagementItemViewModel> items = [];

        foreach (var repositoryWithCommit in repositories)
        {
            var repository = repositoryWithCommit.Item1;
            var lineItem = _factory.MakeViewModel(repository.RepositoryName, repository.RepositoryClonePath, repository.IsHidden);
            lineItem.Branch = _enhanceRepositoryService.GetLocalBranchName(repository.RepositoryClonePath);

            var commit = repositoryWithCommit.Item2;

            if (commit == Commit.DefaultCommit)
            {
                lineItem.HasCommitInformation = false;
            }
            else
            {
                lineItem.HasCommitInformation = true;
                lineItem.LatestCommitAuthor = commit.Author;
                lineItem.LatestCommitSHA = commit.SHA;
                lineItem.MinutesSinceLatestCommit = Convert.ToInt32((DateTime.Now - commit.CommitDateTime).TotalMinutes);
            }

            lineItem.HasAConfigurationFile = repository.HasAConfigurationFile;
            lineItem.MoreOptionsButtonAutomationName = _stringResource.GetLocalized("MoreOptionsAutomationName", repository.RepositoryName);

            items.Add(lineItem);
        }

        return items;
    }

    private async Task<List<Repository>> AssignSourceControlId(List<Repository> repositories)
    {
        foreach (var repository in repositories)
        {
            Guid sourceControlGuid;
            if (!repository.HasAssignedSourceControlProvider)
            {
                sourceControlGuid = await _enhanceRepositoryService.MakeRepositoryEnhanced(repository.RepositoryClonePath);
                _dataAccessService.SetSourceControlId(repository, sourceControlGuid);
            }
        }

        return repositories;
    }

    private List<RepositoryManagementItemViewModel> HideFilterAndSort(List<RepositoryManagementItemViewModel> repositories)
    {
        IEnumerable<RepositoryManagementItemViewModel> filteredAndSortedRepositories = repositories.Where(x => !x.IsHiddenFromPage);

        if (!string.IsNullOrEmpty(FilterText))
        {
            filteredAndSortedRepositories = filteredAndSortedRepositories.Where(x => x.RepositoryName.Contains(FilterText, StringComparison.OrdinalIgnoreCase));
        }

        if (_sortTag == SortOrder.NameAscending)
        {
            filteredAndSortedRepositories = filteredAndSortedRepositories.OrderBy(x => x.RepositoryName);
        }
        else
        {
            filteredAndSortedRepositories = filteredAndSortedRepositories.OrderByDescending(x => x.RepositoryName);
        }

        return filteredAndSortedRepositories.ToList();
    }

    private List<(Repository, Commit)> GetRepositoryAndLatestCommitPairs(List<Repository> repositories)
    {
        var repositoriesButWithCommits = new List<(Repository, Commit)>();

        foreach (var repository in repositories)
        {
            var sourceControlId = repository.HasAssignedSourceControlProvider ? repository.SourceControlClassId ?? Guid.Empty : Guid.Empty;
            var latestCommit = GetLatestCommitInformation(repository.RepositoryClonePath, sourceControlId);

            repositoriesButWithCommits.Add((repository, latestCommit));
        }

        return repositoriesButWithCommits;
    }

    private Commit GetLatestCommitInformation(string repositoryLocation, Guid sourceControlProviderClassId)
    {
        if (sourceControlProviderClassId == Guid.Empty)
        {
            _log.Warning($"sourceControlProviderClassId is guid.empty. {sourceControlProviderClassId}.  Can not get commit information");
            return Commit.DefaultCommit;
        }

        // The call to GetProperties does check the settings for file explorer and souce control integration.
        var repositoryProperties = _enhanceRepositoryService.GetProperties(_commitProperties, repositoryLocation);
        if (!repositoryProperties.TryGetValue("System.VersionControl.LastChangeAuthorName", out var latestCommitAuthorName))
        {
            return Commit.DefaultCommit;
        }

        if (!repositoryProperties.TryGetValue("System.VersionControl.LastChangeDate", out var latestCommitChangedDate))
        {
            return Commit.DefaultCommit;
        }

        if (!repositoryProperties.TryGetValue("System.VersionControl.LastChangeID", out var latestCommitSHA))
        {
            return Commit.DefaultCommit;
        }
        else
        {
            if (latestCommitSHA.ToString().Length > 6)
            {
                latestCommitSHA = latestCommitSHA.ToString().Substring(0, 6);
            }
        }

        DateTime latestCommitDateTime;

        // latestCommitDateTime can be in the future causing diff(now - latestCommitDateTime)
        // to be negative.  Show the negative value.  Be transparent.
        // The future date might have been on purpose.
        if (!DateTime.TryParse(latestCommitChangedDate.ToString(), out latestCommitDateTime))
        {
            latestCommitAuthorName = DateTime.MinValue;
        }

        return new(latestCommitAuthorName.ToString(), latestCommitDateTime, latestCommitSHA.ToString());
    }

    private async Task<string> GetRepositoryLocationFromUser()
    {
        StorageFolder repositoryRootFolder = null;
        try
        {
            using var folderDialog = new WindowOpenFolderDialog();
            repositoryRootFolder = await folderDialog.ShowAsync(_window);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error occurred when selecting a folder for adding a repository.");
            return string.Empty;
        }

        if (repositoryRootFolder == null || string.IsNullOrEmpty(repositoryRootFolder.Path))
        {
            _log.Information("User did not select a location to register");
            return string.Empty;
        }

        _log.Information($"User selected '{repositoryRootFolder.Path}' as location to register");
        return repositoryRootFolder.Path;
    }
}
