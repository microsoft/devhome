// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Database;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DevHome.RepositoryManagement.ViewModels;

public partial class RepositoryManagementMainPageViewModel : IDisposable
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementMainPageViewModel));

    private readonly IHost _host;

    private readonly DevHomeDatabaseContext _databseContext;

    private readonly List<RepositoryManagementItemViewModel> _items;

    public ObservableCollection<RepositoryManagementItemViewModel> Items => new(_items.Where(x => !x.IsHiddenFromPage));

    private static int _myNumber;

    [RelayCommand]
    public void AddExistingRepository()
    {
        var numberToUse = _myNumber++;
        Repository repository = new Repository();
        repository.RepositoryName = $"MicrosoftRepository{numberToUse}";
        repository.LocalBranchName = "main";
        repository.RepositoryClonePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), _myNumber.ToString(CultureInfo.InvariantCulture));

        _databseContext.Add(repository);

        RepositoryMetadata repositoryMetadata = new RepositoryMetadata();
        repositoryMetadata.UtcDateHidden = DateTime.UtcNow;
        repositoryMetadata.IsHiddenFromPage = false;
        repositoryMetadata.RepositoryId = repository.RepositoryId;
        repositoryMetadata.Repository = repository;
        _databseContext.Add(repositoryMetadata);

        repository.RepositoryMetadata = repositoryMetadata;

        _databseContext.SaveChanges();
    }

    public RepositoryManagementMainPageViewModel(IHost host)
    {
        _items = new List<RepositoryManagementItemViewModel>();
        _host = host;
        _databseContext = host.GetService<DevHomeDatabaseContext>();

        var repos = _databseContext.Repositories
            .Include(x => x.RepositoryMetadata)
            .Include(x => x.RemoteCommits);

        foreach (var repo in repos)
        {
            var lineItem = _host.GetService<RepositoryManagementItemViewModel>();
            lineItem.ClonePath = repo.RepositoryClonePath;
            lineItem.Branch = repo.LocalBranchName;
            lineItem.RepositoryName = repo.RepositoryName;
            if (repo.RemoteCommits != null && repo.RemoteCommits.Count > 0)
            {
                var commitAuthor = repo.RemoteCommits[0].Author;
                var commitHash = repo.RemoteCommits[0].CommitHash;
                var commitElapsed = (DateTime.UtcNow - repo.RemoteCommits[0].CommitDateTime).TotalMinutes;
                lineItem.LatestCommit = $"{commitHash} * {commitAuthor} {commitElapsed} minutes";
            }
            else
            {
                lineItem.LatestCommit = "No commits found";
            }

            lineItem.IsHiddenFromPage = repo.RepositoryMetadata.IsHiddenFromPage;
            _items.Add(lineItem);
        }
    }

    /*
    // Some test data to show off in the Repository Management page.
    public void PopulateTestData()
    {
        Items.Clear();

        string repositoryName = string.Empty;
        string clonePath = string.Empty;
        string commitAuthor = string.Empty;
        Guid commitHash = Guid.Empty;
        DateTime commitDate = DateTime.UtcNow;
        for (var x = 0; x < 5; x++)
        {
            repositoryName = $"MicrosoftRepository{x}";
            clonePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), x.ToString(CultureInfo.InvariantCulture));
            commitAuthor = "dhoehna";
            commitHash = Guid.NewGuid();
            commitDate = DateTime.UtcNow;

            var listItem = _host.GetService<RepositoryManagementItemViewModel>();
            listItem.RepositoryName = repositoryName;
            listItem.ClonePath = clonePath;
            listItem.LatestCommit = $"{commitHash} * {commitAuthor} {commitDate}";
            listItem.Branch = "main";
            Items.Add(listItem);

            var repositoryCommit = new RepositoryCommit();
            repositoryCommit.CommitUri = new Uri(clonePath);
            repositoryCommit.Author = commitAuthor;
            repositoryCommit.CommitDateTime = commitDate;
            repositoryCommit.CommitHash = commitHash;

            Repository repository = new Repository();
            repository.RepositoryName = repositoryName;
            repository.LocalBranchName = "main";
            repository.RepositoryClonePath = clonePath;
            repository.RemoteCommits = new List<RepositoryCommit>();
            repository.RemoteCommits.Add(repositoryCommit);

            _repositoryManagementContext.Add(repository);

            DevHome.Database.DatabaseModels.RepositoryManagement.RepositoryManagement repositoryManagement = new Database.DatabaseModels.RepositoryManagement.RepositoryManagement();
            repositoryManagement.UtcDateHidden = DateTime.UtcNow;
            repositoryManagement.IsHiddenFromPage = true;
            repositoryManagement.RepositoryId = repository.RepositoryId;
            repositoryManagement.Repository = repository;
            _repositoryManagementContext.Add(repositoryManagement);
            _repositoryManagementContext.SaveChanges();
        }
    }
    */

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
