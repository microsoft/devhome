// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevHome.Common.TelemetryEvents.DevHomeDatabase;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.Database.Factories;
using DevHome.Telemetry;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DevHome.RepositoryManagement.Services;

public class RepositoryManagementDataAccessService
{
    private const string EventName = "DevHome_RepositoryData_Event";

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementDataAccessService));

    private readonly DevHomeDatabaseContextFactory _databaseContextFactory;

    public RepositoryManagementDataAccessService(DevHomeDatabaseContextFactory databaseContextFactory)
    {
        _databaseContextFactory = databaseContextFactory;
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

        using var dbContext = _databaseContextFactory.GetNewContext();
        dbContext.Add(newRepo);
        dbContext.Add(newMetadata);
    }

    public Repository GetRepository(string repositoryName, string cloneLocation)
    {
        _log.Information("Getting a repository");
        try
        {
            using var dbContext = _databaseContextFactory.GetNewContext();
            return dbContext.Repositories.FirstOrDefault(x => x.RepositoryName.Equals(repositoryName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Path.GetFullPath(x.RepositoryClonePath), Path.GetFullPath(cloneLocation), StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _log.Error(ex, ex.ToString());
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DevHomeDatabaseEvent(nameof(GetRepository), ex));
        }

        return null;
    }

    public bool UpdateCloneLocation(Repository repository, string newLocation)
    {
        repository.RepositoryClonePath = newLocation;
        return true;
    }

    public bool Save()
    {
        try
        {
            using var dbContext = _databaseContextFactory.GetNewContext();
            dbContext.SaveChanges();
            return true;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Exception when saving.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DevHomeDatabaseEvent(nameof(AddRepository), ex));
            return false;
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
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DevHomeDatabaseEvent(nameof(GetRepositories), ex));
        }

        return repositories;
    }
}
