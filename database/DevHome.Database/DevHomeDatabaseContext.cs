// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using DevHome.Common.Helpers;
using DevHome.Common.TelemetryEvents.DevHomeDatabase;
using DevHome.Database.Configurations;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.Database.Services;
using DevHome.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Serilog;
using Windows.Storage;

namespace DevHome.Database;

// TODO: Add documentation around migration and Entity Framework in DevHome.

/// <summary>
/// Provides access to the database for DevHome.
/// </summary>
public class DevHomeDatabaseContext : DbContext, IDevHomeDatabaseContext
{
    // Increment when the schema has changed.
    // Should incremenet once per release.
    public uint SchemaVersion => 1;

    private const string DatabaseFileName = "DevHome.db";

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(DevHomeDatabaseContext));

    public DbSet<Repository> Repositories { get; set; }

    public string DbPath { get; }

    public DevHomeDatabaseContext()
    {
        if (RuntimeHelper.IsMSIX)
        {
            DbPath = Path.Join(ApplicationData.Current.LocalFolder.Path, DatabaseFileName);
        }
        else
        {
            DbPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DatabaseFileName);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        try
        {
            new RepositoryConfiguration().Configure(modelBuilder.Entity<Repository>());
        }
        catch (Exception ex)
        {
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

    public EntityEntry Add(Repository repository)
    {
        return this.Add(repository);
    }
}
