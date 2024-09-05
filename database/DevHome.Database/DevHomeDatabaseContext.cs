// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using DevHome.Common.Helpers;
using DevHome.Common.TelemetryEvents.DevHomeDatabase;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.Telemetry;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Windows.Storage;

namespace DevHome.Database;

public class DevHomeDatabaseContext : DbContext
{
    private const string DatabaseFileName = "DevHome.db";

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(DevHomeDatabaseContext));

    public DbSet<Repository> Repositories { get; set; }

    public DbSet<RepositoryMetadata> RepositoryMetadatas { get; set; }

    public string DbPath { get; }

    public DevHomeDatabaseContext()
    {
        DbPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DatabaseFileName);
        /*
        var figuredOutTheDbPath = false;
        if (RuntimeHelper.IsMSIX)
        {
            try
            {
                DbPath = Path.Join(ApplicationData.Current.LocalFolder.Path, DatabaseFileName);

                _log.Information("database found in Application Data");
                figuredOutTheDbPath = true;
            }
            catch
            {
                _log.Information("Cound not find the database in Application data.");
            }
        }

        if (!figuredOutTheDbPath)
        {
            try
            {
                DbPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DatabaseFileName);
                figuredOutTheDbPath = true;
                _log.Information("Database found in local application data.");
            }
            catch
            {
                _log.Information($"Could not find the database file in local application data.");
            }
        }

        if (!figuredOutTheDbPath)
        {
            _log.Warning($"Cound not find the database file.");
        }

        DbPath = string.Empty;
        */
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO: Use ServiceExtensions as an example to set up individual
        // models using fluent API.  Currently, not needed, but will as this method
        // will expand as more entities are added.
        // If that is too much work these definitions can be placed inside the C# class.
        try
        {
            var repositoryEntity = modelBuilder.Entity<Repository>();
            if (repositoryEntity != null)
            {
                repositoryEntity.Property(x => x.RepositoryClonePath).HasDefaultValue(string.Empty).IsRequired(true);
                repositoryEntity.Property(x => x.RepositoryName).HasDefaultValue(string.Empty).IsRequired(true);
                repositoryEntity.Property(x => x.CreatedUTCDate).HasDefaultValueSql("datetime()");
                repositoryEntity.Property(x => x.UpdatedUTCDate).HasDefaultValue(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                repositoryEntity.ToTable("Repository");
            }

            var repositoryMetadataEntity = modelBuilder.Entity<RepositoryMetadata>();
            if (repositoryMetadataEntity != null)
            {
                repositoryMetadataEntity.Property(x => x.IsHiddenFromPage).HasDefaultValue(false).IsRequired(true);
                repositoryMetadataEntity.Property(x => x.UtcDateHidden).HasDefaultValue(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc)).IsRequired(true);
                repositoryMetadataEntity.Property(x => x.CreatedUTCDate).HasDefaultValueSql("datetime()");
                repositoryMetadataEntity.Property(x => x.UpdatedUTCDate).HasDefaultValue(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                repositoryMetadataEntity.ToTable("RepositoryMetadata");
            }
        }
        catch (Exception ex)
        {
            // TODO: Notify user the database could not initialize.
            _log.Error(ex, "Can not build the database model");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_DatabaseContext_Event",
                LogLevel.Critical,
                new DevHomeDatabaseContextEvent("CreatingModel", ex));
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={DbPath}");
    }
}
