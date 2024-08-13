// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using Microsoft.Extensions.Hosting;

namespace DevHome.RepositoryManagement.ViewModels;

public class RepositoryManagementMainPageViewModel : IDisposable
{
    private readonly IHost _host;

    private readonly RepositoryManagementContext _repositoryManagementContext;

    public ObservableCollection<RepositoryManagementItemViewModel> Items { get; } = new();

    public RepositoryManagementMainPageViewModel(IHost host)
    {
        _host = host;
        _repositoryManagementContext = new RepositoryManagementContext();
    }

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

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
