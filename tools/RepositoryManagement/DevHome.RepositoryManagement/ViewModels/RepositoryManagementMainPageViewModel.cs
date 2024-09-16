// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Services;
using DevHome.Common.Windows.FileDialog;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.Database.Services;
using DevHome.RepositoryManagement.Factories;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.RepositoryManagement.ViewModels;

public partial class RepositoryManagementMainPageViewModel
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementMainPageViewModel));

    private readonly INavigationService _navigationService;

    private readonly RepositoryManagementItemViewModelFactory _factory;

    private readonly RepositoryManagementDataAccessService _dataAccessService;

    private readonly Window _window;

    // _items and Items are renamed in user/dhoehna/TODOsAndUsingTheGitExtension
    // to descriptive names.
    private readonly List<RepositoryManagementItemViewModel> _items = [];

    public ObservableCollection<RepositoryManagementItemViewModel> Items { get; private set; }

    [RelayCommand]
    public async Task AddExistingRepository()
    {
        try
        {
            // TODO: Use extensions to determine if the selected location is a repository.
            // Adding the repository to the database is implemented in
            // user/dhoehna/TODOsAndUsingTheGitExtension
            _log.Information("Opening folder picker to select a new location");
            using var folderPicker = new WindowOpenFolderDialog();
            var newLocation = await folderPicker.ShowAsync(_window);
            if (newLocation != null && newLocation.Path.Length > 0)
            {
                _log.Information($"Selected '{newLocation.Path}' for the repository path.");
            }
            else
            {
                _log.Information("Didn't select a location to clone to");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to open folder picker");
        }
    }

    [RelayCommand]
    public void NavigateToCloneRepositoryExpirence()
    {
        _navigationService.NavigateTo(KnownPageKeys.SetupFlow, KnownPageKeys.RepositoryConfiguration);
    }

    [RelayCommand]
    public void LoadRepositories()
    {
        _items.Clear();
        Items.Clear();
        var repositoriesFromTheDatabase = _dataAccessService.GetRepositories();
        ConvertToLineItems(repositoriesFromTheDatabase).ForEach(x => _items.Add(x));
        _items.Where(x => x.IsHiddenFromPage == false).ToList().ForEach(x => Items.Add(x));
    }

    [RelayCommand]
    public void HideRepository(RepositoryManagementItemViewModel repository)
    {
        if (repository == null)
        {
            return;
        }

        repository.RemoveThisRepositoryFromTheList();
        LoadRepositories();
    }

    public RepositoryManagementMainPageViewModel(
        RepositoryManagementItemViewModelFactory factory,
        RepositoryManagementDataAccessService dataAccessService,
        INavigationService navigationService,
        Window window)
    {
        _dataAccessService = dataAccessService;
        _factory = factory;
        Items = [];
        _navigationService = navigationService;
        _window = window;
    }

    private List<RepositoryManagementItemViewModel> ConvertToLineItems(List<Repository> repositories)
    {
        _log.Information("Converting repositories from the database into view models for display");
        List<RepositoryManagementItemViewModel> items = [];

        foreach (var repo in repositories)
        {
            // TODO: get correct values for branch and latest commit information
            var lineItem = _factory.MakeViewModel(repo.RepositoryName, repo.RepositoryClonePath, repo.IsHidden);
            lineItem.Branch = "Test Value";
            lineItem.LatestCommit = "Test Value";
            lineItem.HasAConfigurationFile = repo.HasAConfigurationFile;
            items.Add(lineItem);
        }

        return items;
    }
}
