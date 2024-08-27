// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using DevHome.Common.TelemetryEvents.DevHomeDatabase;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.Database.Factories;
using DevHome.Telemetry;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DevHome.RepositoryManagement.Services;

public class RepositoryManagementDataAccessService
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementDataAccessService));

    private readonly DevHomeDatabaseContextFactory _databaseContextFactory;

    public RepositoryManagementDataAccessService(DevHomeDatabaseContextFactory databaseContextFactory)
    {
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

        var dbContext = _databaseContextFactory.GetNewContext();
        dbContext.Add(newRepo);
        dbContext.Add(newMetadata);
        try
        {
            dbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Exception when saving a new repository");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DevHomeDatabaseEvent("AddOneRepository", ex));
        }
    }

    public List<Repository> GetRepositories()
    {
        _log.Information("Getting repositories");
        var dbContext = _databaseContextFactory.GetNewContext();

        List<Repository> repositories = [];

        try
        {
            repositories = dbContext.Repositories.Include(x => x.RepositoryMetadata).ToList();
        }
        catch (Exception ex)
        {
            _log.Error(ex, ex.ToString());
            TelemetryFactory.Get<ITelemetry>().LogException(nameof(GetRepositories), ex);
        }

        return repositories;
    }
}
