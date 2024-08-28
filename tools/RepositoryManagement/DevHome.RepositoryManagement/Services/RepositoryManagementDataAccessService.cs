// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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

        var dbContext = _databaseContextFactory.GetNewContext();
        dbContext.Add(newRepo);
        dbContext.Add(newMetadata);
        try
        {
            dbContext.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is SqliteException sqle && sqle.SqliteErrorCode == 19)
            {
                _log.Error(sqle, $"A repository with the name {repositoryName} and clone location {cloneLocation} already exists in the database");
                TelemetryFactory.Get<ITelemetry>().Log(
                    EventName,
                    LogLevel.Critical,
                    new DevHomeDatabaseEvent(nameof(AddRepository), ex));
            }
            else
            {
                _log.Error(ex, "Exception when saving a new repository");
                TelemetryFactory.Get<ITelemetry>().Log(
                    EventName,
                    LogLevel.Critical,
                    new DevHomeDatabaseEvent(nameof(AddRepository), ex));
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Exception when saving a new repository");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DevHomeDatabaseEvent(nameof(AddRepository), ex));
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
