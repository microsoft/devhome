// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.Database.Factories;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DevHome.RepositoryManagement.Services;

public class RepositoryManagementDataAccessService
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementDataAccessService));

    private readonly DatabaseContextFactory _databaseContextFactory;

    public RepositoryManagementDataAccessService(DatabaseContextFactory databaseContextFactory)
    {
        // Store the host to make dbContext and RepositoryManagementItemViewModel
        // dbContext can not be passed in because RepositoryManagementDataAccessService is singleton
        // and dbContext needs to be scoped.
        // RepositoryManagementItemViewModel can not be passed in because multiple are made.
        // The best solution is to make factories for DevHomeContext and RepositoryManagementItemViewModel.
        // Might be in a future change.
        _databaseContextFactory = databaseContextFactory;
    }

    public void AddRepository(string repositoryName, string cloneLocation)
    {
        // Check if repositoryName and cloneLocation is null or empty and
        // return a correct value indicating such.
        Repository newRepo = new();
        newRepo.RepositoryName = repositoryName;
        newRepo.RepositoryClonePath = cloneLocation;

        RepositoryMetadata newMetadata = new();
        newMetadata.Repository = newRepo;
        newMetadata.RepositoryId = newRepo.RepositoryId;
        newMetadata.IsHiddenFromPage = false;

        newRepo.RepositoryMetadata = newMetadata;

        var dbContext = _databaseContextFactory.GetNewDatabaseContext();
        dbContext.Add(newRepo);
        dbContext.Add(newMetadata);
        dbContext.SaveChanges();
    }

    public List<Repository> GetRepositories()
    {
        return QueryDatabaseForRepositories();
    }

    private List<Repository> QueryDatabaseForRepositories()
    {
        var dbContext = _databaseContextFactory.GetNewDatabaseContext();
        return dbContext.Repositories.Include(x => x.RepositoryMetadata).ToList();
    }
}
