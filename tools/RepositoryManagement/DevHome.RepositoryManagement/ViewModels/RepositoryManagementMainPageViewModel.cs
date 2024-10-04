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
using DevHome.Common.TelemetryEvents.RepositoryManagement;
using DevHome.Common.Windows.FileDialog;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.Database.Services;
using DevHome.RepositoryManagement.Factories;
using DevHome.RepositoryManagement.Models;
using DevHome.RepositoryManagement.Services;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Windows.Foundation.Collections;
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

    private readonly IExperimentationService _experimentationService;

    private List<RepositoryManagementItemViewModel> _allLineItems = [];

    private List<Repository> _allRepositoriesFromTheDatabase;

    [ObservableProperty]
    private ObservableCollection<RepositoryManagementItemViewModel> _lineItemsToDisplay;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private bool _areControlsEnabled;

    [ObservableProperty]
    private bool _shouldShowSourceControlSelection;

    [ObservableProperty]
    private bool _isNavigatedTo;

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

        AreControlsEnabled = false;

        UpdateDisplayedRepositories();

        AreControlsEnabled = true;
    }

    [RelayCommand]
    public void FilterRepositories()
    {
        UpdateDisplayedRepositories();
    }

    /// <summary>
    /// Adds an repository on the computer to the database.
    /// </summary>
    /// <returns>An awitable Task</returns>
    /// <remarks>If the repository is already in the database "IsHidden" is set to false.</remarks>
    [RelayCommand]
    public async Task AddExistingRepository()
    {
        AreControlsEnabled = false;

        var existingRepositoryLocation = await GetRepositoryLocationFromUser();
        if (existingRepositoryLocation.Equals(string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            _log.Warning($"Repository in {nameof(AddExistingRepository)} is either empty.");
            AreControlsEnabled = true;
            return;
        }

        var repositoryName = Path.GetFileName(existingRepositoryLocation);
        var maybeExistingRepository = _dataAccessService.GetRepository(repositoryName, existingRepositoryLocation);

        if (maybeExistingRepository != null)
        {
            _dataAccessService.SetIsHidden(maybeExistingRepository, false);
            var existingRepository = _allLineItems.FirstOrDefault(x => x.ClonePath.Equals(existingRepositoryLocation, StringComparison.OrdinalIgnoreCase)
            && x.RepositoryName.Equals(repositoryName, StringComparison.OrdinalIgnoreCase));

            existingRepository.IsHiddenFromPage = false;

            UpdateDisplayedRepositories();
            AreControlsEnabled = true;
            return;
        }

        var foundProvider = false;
        var sourceControlProviderGuid = Guid.Empty;
        foreach (var sourceControlProvider in _enhanceRepositoryService.GetAllSourceControlProviders())
        {
            foundProvider = await _enhanceRepositoryService.MakeRepositoryEnhanced(existingRepositoryLocation, sourceControlProvider);
            if (foundProvider)
            {
                // sourceControlProviderGuid already set to Guid.Empty in case this fails.
                if (Guid.TryParse(sourceControlProvider.ExtensionClassId, out sourceControlProviderGuid))
                {
                    break;
                }
                else
                {
                    _log.Warning($"The valid source control provider id {sourceControlProvider.ExtensionClassId} could not be parsed into a string.");
                    foundProvider = false;
                }
            }
        }

        var repositoryUrl = _enhanceRepositoryService.GetRepositoryUrl(existingRepositoryLocation);
        var configurationFileLocation = DscHelpers.GetConfigurationFileIfExists(existingRepositoryLocation);
        var newRepository = _dataAccessService.MakeRepository(
            repositoryName,
            existingRepositoryLocation,
            configurationFileLocation,
            repositoryUrl,
            sourceControlProviderGuid);

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

        AreControlsEnabled = true;
    }

    [RelayCommand]
    public void NavigateToCloneRepositoryExpirence()
    {
        _navigationService.NavigateTo(KnownPageKeys.SetupFlow, KnownPageKeys.RepositoryConfiguration);
    }

    [RelayCommand]
    public async Task LoadRepositories()
    {
        IsNavigatedTo = true;
        AreControlsEnabled = false;

        var tempLineItemsToDisplay = new List<RepositoryManagementItemViewModel>();
        await Task.Run(async () =>
        {
            _allRepositoriesFromTheDatabase = _dataAccessService.GetRepositories();
            _allRepositoriesFromTheDatabase = await AssignSourceControlId(_allRepositoriesFromTheDatabase);

            var repositoriesWithCommits = GetRepositoryAndLatestCommitPairs(_allRepositoriesFromTheDatabase);

            _allLineItems.Clear();
            _allLineItems = ConvertToLineItems(repositoriesWithCommits);
            tempLineItemsToDisplay = HideFilterAndSort(_allLineItems).Where(x => x.IsHiddenFromPage == false).ToList();
        });

        ShouldShowSourceControlSelection = _experimentationService.IsFeatureEnabled("RepositoryManagementSourceControlSelector");

        LineItemsToDisplay.Clear();
        LineItemsToDisplay = new(tempLineItemsToDisplay);

        AreControlsEnabled = true;
        IsNavigatedTo = false;
    }

    /// <summary>
    /// Removes the repository from the PC.
    /// </summary>
    /// <param name="repositoryLineItem">The line item acted upon.</param>
    /// <returns>An awaitable Task</returns>
    /// <remarks>Even with an update callback this method can't update _allLineItems.
    /// This means a call to UpdateDisplayedRepositories won't change the UI because the line item
    /// isn't removed from _lineItemsToDisplay.</remarks>
    [RelayCommand]
    public async Task DeleteRepositoryAsync(RepositoryManagementItemViewModel repositoryLineItem)
    {
        var repositoryName = repositoryLineItem.RepositoryName;
        var clonePath = repositoryLineItem.ClonePath;
        var deleteRepositoryConfirmationDialog = new ContentDialog()
        {
            XamlRoot = _window.Content.XamlRoot,
            Title = _stringResource.GetLocalized("DeleteRepositoryDialogTitle", repositoryName, clonePath),
            Content = _stringResource.GetLocalized("DeleteRepositoryDialogContent"),
            PrimaryButtonText = _stringResource.GetLocalized("Yes"),
            CloseButtonText = _stringResource.GetLocalized("Cancel"),
        };

        ContentDialogResult dialogResult = ContentDialogResult.None;

        try
        {
            dialogResult = await deleteRepositoryConfirmationDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to open delete confirmation dialog.");

            // Keep LineItemErrorEvent because the event did come from the line item.
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_RepositoryLineItem_Event",
                LogLevel.Critical,
                new RepositoryLineItemErrorEvent(nameof(DeleteRepositoryAsync), ex));
        }

        if (dialogResult != ContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            var repository = _dataAccessService.GetRepository(repositoryName, clonePath);
            if (repository is null)
            {
                _log.Warning($"The repository with name {repositoryName} and clone location {clonePath} is not in the database when it is expected to be there.");
                TelemetryFactory.Get<ITelemetry>().Log(
                    "DevHome_RepositoryLineItem_Event",
                    LogLevel.Critical,
                    new RepositoryLineItemEvent(nameof(DeleteRepositoryAsync)));
            }
            else
            {
                _dataAccessService.RemoveRepository(repository);
            }
        }
        catch (Exception ex)
        {
            // Fall through to removing files and folders.
            _log.Error(ex, $"Error when removing the repository from the database.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_RepositoryLineItem_Event",
                LogLevel.Critical,
                new RepositoryLineItemErrorEvent(nameof(DeleteRepositoryAsync), ex));
        }

        try
        {
            RepositoryActionHelper.DeleteEverything(clonePath);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error when deleting the repository.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_RepositoryLineItem_Event",
                LogLevel.Critical,
                new RepositoryLineItemErrorEvent(nameof(DeleteRepositoryAsync), ex));
        }

        try
        {
            _enhanceRepositoryService.RemoveTrackedRepository(clonePath);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error when removing the repository from tracking.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_RepositoryLineItem_Event",
                LogLevel.Critical,
                new RepositoryLineItemErrorEvent(nameof(DeleteRepositoryAsync), ex));
        }

        AreControlsEnabled = false;
        _allLineItems.Remove(repositoryLineItem);
        UpdateDisplayedRepositories();
        AreControlsEnabled = true;
    }

    public RepositoryManagementMainPageViewModel(
        RepositoryManagementItemViewModelFactory factory,
        RepositoryManagementDataAccessService dataAccessService,
        INavigationService navigationService,
        RepositoryEnhancerService enchanceRepositoryService,
        Window window,
        IExperimentationService experimentationService)
    {
        _dataAccessService = dataAccessService;
        _factory = factory;
        LineItemsToDisplay = [];
        _navigationService = navigationService;
        _enhanceRepositoryService = enchanceRepositoryService;
        _window = window;
        _experimentationService = experimentationService;
    }

    /// <summary>
    /// Updates Repository Management UI.
    /// </summary>
    /// <remarks>For the change to appear, make sure modifications are done to the view model.</remarks>
    private void UpdateDisplayedRepositories()
    {
        LineItemsToDisplay.Clear();
        var lineItemsToShow = HideFilterAndSort(_allLineItems);
        lineItemsToShow.ForEach(x => LineItemsToDisplay.Add(x));
    }

    private List<RepositoryManagementItemViewModel> ConvertToLineItems(List<(Repository, Commit)> repositories)
    {
        var sourceControlProviders = _enhanceRepositoryService.GetAllSourceControlProviders();
        _log.Information("Converting repositories from the database into view models for display");
        List<RepositoryManagementItemViewModel> items = [];

        foreach (var repositoryWithCommit in repositories)
        {
            var repository = repositoryWithCommit.Item1;
            var lineItem = _factory.MakeViewModel(
                repository.RepositoryName,
                repository.RepositoryClonePath,
                repository.IsHidden,
                UpdateDisplayedRepositories);

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

                var commitInMinutes = Convert.ToInt32((DateTime.Now - commit.CommitDateTime).TotalMinutes);
                var minutString = _stringResource.GetLocalized("MinuteAbbreviation");
                lineItem.MinutesSinceLatestCommit = $"{commitInMinutes} {minutString}";
            }

            var sourceControlProvider = sourceControlProviders.SingleOrDefault(x => Guid.Parse(x.ExtensionClassId) == repository.SourceControlClassId);
            if (sourceControlProvider != null)
            {
                lineItem.SourceControlExtensionClassId = sourceControlProvider.ExtensionClassId;
                lineItem.SourceControlProviderDisplayName = sourceControlProvider.ExtensionDisplayName;
                lineItem.SourceControlProviderPackageDisplayName = sourceControlProvider.PackageFullName;
            }
            else
            {
                lineItem.SourceControlExtensionClassId = Guid.Empty.ToString();
                lineItem.SourceControlProviderDisplayName = _stringResource.GetLocalized("UnassignedSourceControlProvider");
                lineItem.SourceControlProviderPackageDisplayName = _stringResource.GetLocalized("UnassignedSourceControlProvider");
            }

            lineItem.HasAConfigurationFile = repository.HasAConfigurationFile;
            lineItem.MoreOptionsButtonAutomationName = _stringResource.GetLocalized("MoreOptionsAutomationName", repository.RepositoryName);

            items.Add(lineItem);
        }

        return items;
    }

    private async Task<List<Repository>> AssignSourceControlId(List<Repository> repositories)
    {
        var sourceControlProviders = _enhanceRepositoryService.GetAllSourceControlProviders();

        foreach (var repository in repositories)
        {
            if (!repository.HasAssignedSourceControlProvider)
            {
                var foundExtension = false;
                foreach (var extension in sourceControlProviders)
                {
                    foundExtension = await _enhanceRepositoryService.MakeRepositoryEnhanced(repository.RepositoryClonePath, extension);
                    var sourceControlExtensionId = Guid.Empty;
                    if (foundExtension && Guid.TryParse(extension.ExtensionClassId, out sourceControlExtensionId))
                    {
                        _dataAccessService.SetSourceControlId(repository, sourceControlExtensionId);
                    }
                    else
                    {
                        _log.Warning($"Could not assign source control {extension.ExtensionDisplayName} to the repository");
                    }
                }
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

        IPropertySet repositoryProperties = new PropertySet();

        try
        {
            repositoryProperties = _enhanceRepositoryService.GetProperties(_commitProperties, repositoryLocation);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error with getting properties from the repository");
        }

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
