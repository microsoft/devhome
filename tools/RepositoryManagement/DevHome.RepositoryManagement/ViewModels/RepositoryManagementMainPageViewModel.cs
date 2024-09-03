// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.RepositoryManagement.Services;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DevHome.RepositoryManagement.ViewModels;

public partial class RepositoryManagementMainPageViewModel
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementMainPageViewModel));

    private readonly IHost _host;

    private readonly RepositoryManagementDataAccessService _dataAccessService;

    public ObservableCollection<RepositoryManagementItemViewModel> Items => new(_dataAccessService.GetRepositories(true));

    [RelayCommand]
    public void AddExistingRepository()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public void LoadRepositories()
    {
        Items.Clear();
        var repositoriesFromTheDatabase = _dataAccessService.GetRepositories();
        ConvertToLineItems(repositoriesFromTheDatabase).ForEach(x => Items.Add(x));
    }

    public RepositoryManagementMainPageViewModel(IHost host, RepositoryManagementDataAccessService dataAccessService)
    {
        _dataAccessService = dataAccessService;
        _host = host;
        Items = [];
    }

    private List<RepositoryManagementItemViewModel> ConvertToLineItems(List<Repository> repositories)
    {
        _log.Information("Converting repositories from the database into view models for display");
        List<RepositoryManagementItemViewModel> items = new();

        foreach (var repo in repositories)
        {
            var lineItem = _host.GetService<RepositoryManagementItemViewModel>();
            lineItem.ClonePath = repo.RepositoryClonePath;
            lineItem.Branch = "main"; // Test value.  Will change in the future.
            lineItem.RepositoryName = repo.RepositoryName;
            lineItem.LatestCommit = "No commits found"; // Test value.  Will change in the future.

            lineItem.IsHiddenFromPage = repo.RepositoryMetadata.IsHiddenFromPage;
            items.Add(lineItem);
        }

        return items;
    }
}
