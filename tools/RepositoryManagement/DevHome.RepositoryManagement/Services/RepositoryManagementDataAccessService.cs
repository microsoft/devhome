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

    public Repository MakeRepository(string repositoryName, string cloneLocation)
    {
        var existingRepository = GetRepository(repositoryName, cloneLocation);
        if (existingRepository != null)
        {
            _log.Information($"A Repository with name {repositoryName} and clone location {cloneLocation} exists in the repository already.");
            return existingRepository;
        }

        Repository newRepo = new()
        {
            RepositoryName = repositoryName,
            RepositoryClonePath = cloneLocation,
        };

        RepositoryMetadata newMetadata = new()
        {
            Repository = newRepo,
            RepositoryId = newRepo.RepositoryId,
            IsHiddenFromPage = false,
        };

        newRepo.RepositoryMetadata = newMetadata;

        return newRepo;
    }

    public List<Repository> GetRepositories()
    {
        _log.Information("Getting repositories");
        List<Repository> repositories = [];

        try
        {
            using var dbContext = _databaseContextFactory.GetNewContext();
            repositories = [.. dbContext.Repositories.Include(x => x.RepositoryMetadata)];
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

    public Repository GetRepository(string repositoryName, string cloneLocation)
    {
        _log.Information("Getting a repository");
        try
        {
            using var dbContext = _databaseContextFactory.GetNewContext();
#pragma warning disable CA1309 // Use ordinal string comparison
            return dbContext.Repositories.FirstOrDefault(x => x.RepositoryName.Equals(repositoryName)
            && string.Equals(x.RepositoryClonePath, Path.GetFullPath(cloneLocation)));
#pragma warning restore CA1309 // Use ordinal string comparison
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

    public bool Save(Repository repository)
    {
        try
        {
            using var dbContext = _databaseContextFactory.GetNewContext();

            var maybeExistingRepository = GetRepository(repository.RepositoryName, repository.RepositoryClonePath);
            if (maybeExistingRepository != null)
            {
                dbContext.Update(repository);
            }
            else
            {
                dbContext.Add(repository);
            }

            dbContext.SaveChanges();
            return true;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Exception when saving.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DevHomeDatabaseEvent(nameof(Save), ex));
            return false;
        }
    }
}
