// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.Database.Services;
using DevHome.RepositoryManagement.Factories;
using Serilog;

namespace DevHome.RepositoryManagement.ViewModels;

public partial class RepositoryManagementMainPageViewModel
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementMainPageViewModel));

    private readonly RepositoryManagementItemViewModelFactory _factory;

    private readonly RepositoryManagementDataAccessService _dataAccessService;

    private readonly List<RepositoryManagementItemViewModel> _items = [];

    public ObservableCollection<RepositoryManagementItemViewModel> Items { get; private set; }

    [RelayCommand]
    public void AddExistingRepository()
    {
        throw new NotImplementedException();
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

    public RepositoryManagementMainPageViewModel(
        RepositoryManagementItemViewModelFactory factory,
        RepositoryManagementDataAccessService dataAccessService)
    {
        _dataAccessService = dataAccessService;
        _factory = factory;
        Items = [];
    }

    private List<RepositoryManagementItemViewModel> ConvertToLineItems(List<Repository> repositories)
    {
        _log.Information("Converting repositories from the database into view models for display");
        List<RepositoryManagementItemViewModel> items = [];

        foreach (var repo in repositories)
        {
            // TODO: get correct values for branch and latest commit information
            var lineItem = _factory.MakeViewModel(repo.RepositoryName, repo.RepositoryClonePath, repo.IsHidden);
            lineItem.Branch = "main"; // Test value.  Will change in the future.
            lineItem.LatestCommit = "No commits found"; // Test value.  Will change in the future.
            lineItem.HasAConfigurationFile = repo.HasAConfigurationFile;
            items.Add(lineItem);
        }

        return items;
    }
}
