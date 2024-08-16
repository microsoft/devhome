// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Database;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.RepositoryManagement.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Hosting;

namespace DevHome.RepositoryManagement.Services;

public class RepositoryManagementDataAccessService
{
    private readonly IHost _host;

    public RepositoryManagementDataAccessService(IHost host)
    {
        _host = host;
    }

    public void AddRepository(string repositoryName, string cloneLocation, string branch)
    {
        Repository newRepo = new();
        newRepo.RepositoryName = repositoryName;
        newRepo.RepositoryClonePath = cloneLocation;
        newRepo.LocalBranchName = branch;

        RepositoryMetadata newMetadata = new();
        newMetadata.Repository = newRepo;
        newMetadata.RepositoryId = newRepo.RepositoryId;
        newMetadata.IsHiddenFromPage = false;

        var dbContext = _host.GetService<DevHomeDatabaseContext>();
        dbContext.Add(newRepo);
        dbContext.Add(newMetadata);
        dbContext.SaveChanges();
    }

    public List<RepositoryManagementItemViewModel> GetRepositories()
    {
        var repos = QueryDatbaseForRepositories();
        return ConvertToLineItems(repos);
    }

    private IIncludableQueryable<Repository, List<RepositoryCommit>> QueryDatbaseForRepositories()
    {
        var dbContext = _host.GetService<DevHomeDatabaseContext>();
        return dbContext.Repositories
            .Include(x => x.RepositoryMetadata)
            .Include(x => x.RemoteCommits);
    }

    private List<RepositoryManagementItemViewModel> ConvertToLineItems(IIncludableQueryable<Repository, List<RepositoryCommit>> repositories)
    {
        List<RepositoryManagementItemViewModel> items = new();

        foreach (var repo in repositories)
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
            items.Add(lineItem);
        }

        return items;
    }
}
