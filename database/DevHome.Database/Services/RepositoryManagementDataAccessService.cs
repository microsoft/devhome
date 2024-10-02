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
using Serilog;

namespace DevHome.Database.Services;

/// <summary>
/// Provides actions to CRUD <see cref="Repository"/>'s.  This will update UpdatedUTCDate.
/// </summary>
public class RepositoryManagementDataAccessService
{
    private const string EventName = "DevHome_RepositoryData_Event";

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementDataAccessService));

    private readonly IDevHomeDatabaseContextFactory _databaseContextFactory;

    public RepositoryManagementDataAccessService(IDevHomeDatabaseContextFactory databaseContextFactory)
    {
        _databaseContextFactory = databaseContextFactory;
    }

    /// <summary>
    /// Makes a new <see cref="Repository"/>.
    /// </summary>
    /// <param name="repositoryName">The name of the repository.</param>
    /// <param name="cloneLocation">The full path to the root of the repository.</param>
    /// <param name="repositoryUri">The uri used to clone the repository.</param>
    /// <returns>The newly made Repository.  Null if an exception occured.  Can return a repository
    /// from the database if it already exists.</returns>
    public Repository? MakeRepository(string repositoryName, string cloneLocation, string repositoryUri)
    {
        return MakeRepository(repositoryName, cloneLocation, string.Empty, repositoryUri);
    }

    /// <summary>
    /// Makes a new repository and incudes information about the configuration file.
    /// </summary>
    /// <param name="repositoryName">The name of the repository.</param>
    /// <param name="cloneLocation">The full path to the root of the repository.</param>
    /// <param name="configurationFileLocationAndName">Full path, including the file name, of the configuration file.</param>
    /// <param name="repositoryUri">The uri used to clone the repository.</param>
    /// <returns>The newly made Repository.  Null if an exception occured.  Can return a repository
    /// from the database if it already exists.</returns>
    public Repository? MakeRepository(string repositoryName, string cloneLocation, string configurationFileLocationAndName, string repositoryUri)
    {
        return MakeRepository(repositoryName, cloneLocation, configurationFileLocationAndName, repositoryUri, null);
    }

    public Repository MakeRepository(string repositoryName, string cloneLocation, string configurationFileLocationAndName, string repositoryUri, Guid? sourceControlProviderClassId)
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
            RepositoryUri = repositoryUri,
            SourceControlClassId = sourceControlProviderClassId,
        };

        if (!string.IsNullOrEmpty(configurationFileLocationAndName))
        {
            if (!File.Exists(configurationFileLocationAndName))
            {
                _log.Information($"No file exists at {configurationFileLocationAndName}.  This repository will not have a configuration file.");
            }
            else
            {
                newRepo.ConfigurationFileLocation = configurationFileLocationAndName;
            }
        }

        try
        {
            using var dbContext = _databaseContextFactory.GetNewContext();
            dbContext.Add(newRepo);
            dbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Exception when saving in {nameof(MakeRepository)}");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DevHomeDatabaseEvent(nameof(MakeRepository), ex));
            return new Repository();
        }

        return newRepo;
    }

    /// <summary>
    /// Gets all repositories stored in the database.
    /// </summary>
    /// <returns>A list of all repositories found in the database.</returns>
    public List<Repository> GetRepositories()
    {
        _log.Information("Getting repositories");
        List<Repository> repositories = [];

        try
        {
            using var dbContext = _databaseContextFactory.GetNewContext();
            repositories = [.. dbContext.Repositories];
        }
        catch (Exception ex)
        {
            _log.Error(ex, ex.ToString());
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DatabaseEvent(nameof(GetRepositories), ex));
        }

        return repositories;
    }

    /// <summary>
    /// Retrives a single <see cref="Repository"/> from the database.
    /// </summary>
    /// <param name="repositoryName">The name of the repository.</param>
    /// <param name="cloneLocation">The full path to the root of the repository.</param>
    /// <returns>If found, the <see cref="Repository"/>.  Otherwise null.</returns>
    public Repository? GetRepository(string repositoryName, string cloneLocation)
    {
        _log.Information("Getting a repository");
        try
        {
            using var dbContext = _databaseContextFactory.GetNewContext();
#pragma warning disable CA1309 // Use ordinal string comparison
            // https://learn.microsoft.com/ef/core/miscellaneous/collations-and-case-sensitivity#translation-of-built-in-net-string-operations
            return dbContext.Repositories.FirstOrDefault(x => x.RepositoryName!.Equals(repositoryName)
            && string.Equals(x.RepositoryClonePath, Path.GetFullPath(cloneLocation)));
#pragma warning restore CA1309 // Use ordinal string comparison
        }
        catch (Exception ex)
        {
            _log.Error(ex, ex.ToString());
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DatabaseEvent(nameof(GetRepository), ex));
        }

        return null;
    }

    /// <summary>
    /// Updates the clone location of a <see cref="Repository"/>
    /// </summary>
    /// <param name="repository">The repository to update.</param>
    /// <param name="newLocation">The new clone location</param>
    /// <returns>True if the update was successful.  Otherwise false.</returns>
    public bool UpdateCloneLocation(Repository repository, string newLocation)
    {
        try
        {
            repository.RepositoryClonePath = newLocation;

            if (repository.HasAConfigurationFile)
            {
                var configurationFolder = Path.GetDirectoryName(repository.ConfigurationFileLocation);
                var configurationFileName = Path.GetFileName(configurationFolder);

                repository.ConfigurationFileLocation = Path.Combine(newLocation, configurationFolder ?? string.Empty, configurationFileName ?? string.Empty);
            }

            repository.UpdatedUTCDate = DateTime.UtcNow;

            using var dbContext = _databaseContextFactory.GetNewContext();
            dbContext.Repositories.Update(repository);
            dbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Exception when updating the clone location.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DatabaseEvent(nameof(UpdateCloneLocation), ex));
            return false;
        }

        return true;
    }

    public bool SetSourceControlId(Repository repository, Guid sourceControlId)
    {
        try
        {
            using var dbContext = _databaseContextFactory.GetNewContext();
            var repositoryToUpdate = dbContext.Repositories.Find(repository.RepositoryId);
            if (repositoryToUpdate == null)
            {
                _log.Warning($"{nameof(UpdateCloneLocation)} was called with a RepositoryId of {repository.RepositoryId} and it does not exist in the database.");
                return false;
            }

            // TODO: Figure out a method to update the entity in the database and
            // the entity in memory.
            repository.SourceControlClassId = sourceControlId;
            repositoryToUpdate.SourceControlClassId = sourceControlId;

            dbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Exception when updating the clone location.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DatabaseEvent(nameof(UpdateCloneLocation), ex));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Sets the IsHidden property of the <see cref="Repository"/>.
    /// </summary>
    /// <param name="repository">The repository to update.</param>
    /// <param name="isHidden">The value to put into the database.</param>
    public void SetIsHidden(Repository repository, bool isHidden)
    {
        try
        {
            repository.IsHidden = isHidden;
            repository.UpdatedUTCDate = DateTime.UtcNow;

            using var dbContext = _databaseContextFactory.GetNewContext();
            dbContext.Repositories.Update(repository);
            dbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Exception when setting repository hidden status.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DatabaseEvent(nameof(UpdateCloneLocation), ex));
            return;
        }
    }

    /// <summary>
    /// Removes the <see cref="Repository"/> from the database.
    /// </summary>
    /// <param name="repository">The repository to remove.</param>
    public void RemoveRepository(Repository repository)
    {
        try
        {
            using var dbContext = _databaseContextFactory.GetNewContext();
            dbContext.Repositories.Remove(repository);
            dbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Exception when removing the repository.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DatabaseEvent(nameof(UpdateCloneLocation), ex));
            return;
        }
    }
}
