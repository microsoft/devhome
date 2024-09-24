// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using DevHome.Common.Helpers;
using DevHome.Common.TelemetryEvents.DevHomeDatabase;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.Database.Services;
using DevHome.Telemetry;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Windows.Storage;

namespace DevHome.Database;

/// <summary>
/// To make the database please run the following in Package Manager Console
/// Update-Database -StartupProject DevHome.Database -Project DevHome.Database
///
/// TODO: Remove this comment after database migration is implemeneted.
/// TODO: Add documentation around migration and Entity Framework in DevHome.
/// </summary>
public class DevHomeDatabaseContext : DbContext
{
    private const uint SchemaVersion = 1;

    private const string DatabaseFileName = "DevHome";

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(DevHomeDatabaseContext));

    private readonly ISchemaValidator _schemaValidator;

    private uint _previousSchemaVersion;

    public DbSet<Repository> Repositories { get; set; }

    public string DbPath { get; }

#if CANARY_BUILD
    private const string DevHomeNameExtension = "_canary.db";
#elif STABLE_BUILD
    private const string DevHomeNameExtension = "_stable.db";
#else
    private const string DevHomeNameExtension = "_dev.db";
#endif

    public DevHomeDatabaseContext()
    {
        if (RuntimeHelper.IsMSIX)
        {
            DbPath = Path.Join(ApplicationData.Current.LocalFolder.Path, DatabaseFileName + DevHomeNameExtension);
        }
        else
        {
            DbPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DatabaseFileName + DevHomeNameExtension);
        }

        _schemaValidator = SchemaValidatorFactory.MakeValidator();
    }

    public bool ShouldPerformDatabaseMigration()
    {
        if (!File.Exists(DbPath) || !_schemaValidator.DoesPreviousSchemaExist())
        {
            // previous version will stay at 0.
            // causing a migration from 0 to SchemaVersion.
            return true;
        }

        var contents = _schemaValidator.GetPreviousSchemaVersion(_schemaValidator.GetPreviousSchema());
        if (contents != SchemaVersion)
        {
            _previousSchemaVersion = contents;
            return true;
        }

        return false;
    }

    public void MigrateDatabaseIfNeeded()
    {
        if (!ShouldPerformDatabaseMigration())
        {
            return;
        }

        string createDatabaseQuery;
        if (RuntimeHelper.IsMSIX)
        {
            // Database does not exist.  Make the most recent version
            Uri uri = new Uri($"ms-appx:///Assets/MigrationScripts/{_previousSchemaVersion}To{SchemaVersion}.sql");
            var migrationStorageFile = StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().Result;
            createDatabaseQuery = FileIO.ReadTextAsync(migrationStorageFile).AsTask().Result.ToString();
        }
        else
        {
            createDatabaseQuery = File.ReadAllText($"Assets/MigrationScripts/{_previousSchemaVersion}To{SchemaVersion}.sql");
        }

        if (!string.IsNullOrEmpty(createDatabaseQuery))
        {
            Database.ExecuteSqlRaw(createDatabaseQuery);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO: Use ServiceExtensions as an example to set up individual
        // models using fluent API.  Currently, not needed, but will as this method
        // will expand as more entities are added.
        // If that is too much work these definitions can be placed inside the C# class.
        try
        {
            // TODO: How to update "UpdatedAt"?
            var repositoryEntity = modelBuilder.Entity<Repository>();
            if (repositoryEntity != null)
            {
                repositoryEntity.Property(x => x.ConfigurationFileLocation).HasDefaultValue(string.Empty);
                repositoryEntity.Property(x => x.RepositoryClonePath).HasDefaultValue(string.Empty).IsRequired(true);
                repositoryEntity.Property(x => x.RepositoryName).HasDefaultValue(string.Empty).IsRequired(true);
                repositoryEntity.Property(x => x.CreatedUTCDate).HasDefaultValueSql("datetime()");
                repositoryEntity.Property(x => x.UpdatedUTCDate).HasDefaultValueSql("datetime()");
                repositoryEntity.Property(x => x.RepositoryUri).HasDefaultValue(string.Empty);
                repositoryEntity.ToTable("Repository");
            }
        }
        catch (Exception ex)
        {
            // TODO: Notify user the database could not initialize.
            _log.Error(ex, "Can not build the database model");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_DatabaseContext_Event",
                LogLevel.Critical,
                new DatabaseContextErrorEvent("CreatingModel", ex));
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={DbPath}");
    }
}
