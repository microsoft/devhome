// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using DevHome.Common.TelemetryEvents.DevHomeDatabase;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.Telemetry;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Windows.Storage;

namespace DevHome.Database;

/// <summary>
/// Please surround calls to .SaveChanges() with try/catch
/// </summary>
public class DevHomeDatabaseContext : DbContext
{
    private const string DatabaseFileName = "DevHome.db";

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(DevHomeDatabaseContext));

    public DbSet<Repository> Repositories { get; set; }

    public DbSet<RepositoryMetadata> RepositoryMetadatas { get; set; }

    public string DbPath { get; }

    public DevHomeDatabaseContext()
    {
        // Add-Migration and update-Migration fails when this is in the appx path.
        // Need to think of something better besides local app data.
        // Because dotnet.exe does not have access to the package/user location.
        DbPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DatabaseFileName);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // This should be split like service extensions.
        // As more entities are added, the longer this method will get.
        try
        {
            // According to learn.microsoft the below methods do not throw.
            // Catch and log an exception in case.
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
            // Show a dialog box then close DevHome.
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
