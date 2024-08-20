// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevHome.Common.Extensions;
using DevHome.Database;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.RepositoryManagement.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DevHome.RepositoryManagement.Services;

public class RepositoryManagementDataAccessService
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementDataAccessService));

    private readonly IHost _host;

    public RepositoryManagementDataAccessService(IHost host)
    {
        // Store the host to make dbContext and RepositoryManagementItemViewModel
        // dbContext can not be passed in because RepositoryManagementDataAccessService is singleton
        // and dbContext needs to be scoped.
        // RepositoryManagementItemViewModel can not be passed in because multiple are made.
        _host = host;
    }

    public void AddRepository(string repositoryName, string cloneLocation)
    {
        Repository newRepo = new();
        newRepo.RepositoryName = repositoryName;
        newRepo.RepositoryClonePath = cloneLocation;

        RepositoryMetadata newMetadata = new();
        newMetadata.Repository = newRepo;
        newMetadata.RepositoryId = newRepo.RepositoryId;
        newMetadata.IsHiddenFromPage = false;

        newRepo.RepositoryMetadata = newMetadata;

        var dbContext = _host.GetService<DevHomeDatabaseContext>();
        dbContext.Add(newRepo);
        dbContext.Add(newMetadata);
        dbContext.SaveChanges();
    }

    public List<RepositoryManagementItemViewModel> GetRepositories(bool removeRepositoriesNotInTheirSavedLocation)
    {
        var repos = QueryDatabaseForRepositories(removeRepositoriesNotInTheirSavedLocation);
        return ConvertToLineItems(repos);
    }

    private IIncludableQueryable<Repository, RepositoryMetadata> QueryDatabaseForRepositories(bool removeRepositoriesNotInTheirSavedLocation)
    {
        var dbContext = _host.GetService<DevHomeDatabaseContext>();
        var repositoriesFromDatabase = dbContext.Repositories.Include(x => x.RepositoryMetadata);

        if (!removeRepositoriesNotInTheirSavedLocation)
        {
            return repositoriesFromDatabase;
        }

        // The database can get out of sync.  Take this as an example.
        // DevHome writes a repository.
        // The user moves or deletes the repository.
        // Now the database record has the correct name, but the incorrect location.
        // Remove records where the directory does not exist.
        repositoriesFromDatabase
            .ToList()
            .Where(x => !Directory.Exists(x.RepositoryClonePath))
            .ToList()
            .ForEach(x => dbContext.Repositories.Remove(x));

        dbContext.SaveChanges();

        return dbContext.Repositories.Include(x => x.RepositoryMetadata);
    }

    private List<RepositoryManagementItemViewModel> ConvertToLineItems(IIncludableQueryable<Repository, RepositoryMetadata> repositories)
    {
        List<RepositoryManagementItemViewModel> items = new();

        foreach (var repo in repositories)
        {
            var lineItem = _host.GetService<RepositoryManagementItemViewModel>();
            lineItem.ClonePath = repo.RepositoryClonePath;
            lineItem.Branch = "main";
            lineItem.RepositoryName = repo.RepositoryName;
            lineItem.LatestCommit = "No commits found";

            lineItem.IsHiddenFromPage = repo.RepositoryMetadata.IsHiddenFromPage;
            items.Add(lineItem);
        }

        return items;
    }
}
