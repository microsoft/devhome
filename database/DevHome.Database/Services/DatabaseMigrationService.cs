// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using DevHome.Common.Helpers;
using DevHome.Common.TelemetryEvents.DevHomeDatabase;
using DevHome.Database.Factories;
using DevHome.Telemetry;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Windows.Storage;

namespace DevHome.Database.Services;

public class DatabaseMigrationService
{
    private static bool _hasMigrationRan;

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(DatabaseMigrationService));

    private readonly ISchemaAccessFactory _schemaAccessFactory;

    private readonly IDevHomeDatabaseContextFactory _databaseContextFactory;

    private readonly ICustomMigrationHandlerFactory _customMigrationHandlerFactory;

    public DatabaseMigrationService(
        ISchemaAccessFactory schemaAccessFactory,
        IDevHomeDatabaseContextFactory databaseContextFactory,
        ICustomMigrationHandlerFactory customMigrationHandlerFactory)
    {
        _schemaAccessFactory = schemaAccessFactory;
        _databaseContextFactory = databaseContextFactory;
        _customMigrationHandlerFactory = customMigrationHandlerFactory;
    }

    public bool ShouldMigrateDatabase()
    {
        var schemaAccessor = _schemaAccessFactory.GenerateSchemaAccessor();
        var previousVersion = schemaAccessor.GetPreviousSchemaVersion();
        var currentVersion = _databaseContextFactory.GetNewContext().SchemaVersion;

        return previousVersion != currentVersion;
    }

    /// <summary>
    /// Migrates the database to the version stored inside the database context.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the migration was ran more than once per session.</exception>
    public void MigrateDatabase()
    {
        // TODO: Make sure DevHome can still run even if the migration is not performed.
        // Check here even if ShouldMigrateDatabase was called already.
        // Just to be sure.
        if (!ShouldMigrateDatabase())
        {
            return;
        }

        if (_hasMigrationRan)
        {
            _log.Error($"Migration should not run more than once.");
            throw new InvalidOperationException($"Migration should not run more than once per session.");
        }

        // Set here to prevent multiple runs if the previous migration failed.
        _hasMigrationRan = true;

        var schemaAccessor = _schemaAccessFactory.GenerateSchemaAccessor();
        var previousVersion = schemaAccessor.GetPreviousSchemaVersion();
        var currentVersion = _databaseContextFactory.GetNewContext().SchemaVersion;

        var migrateDatabaseScript = GetMigrationQuery(previousVersion, currentVersion);

        if (string.IsNullOrEmpty(migrateDatabaseScript))
        {
            Log.Warning($"The migration script is empty.  Not migrating the database.");
            return;
        }

        var databaseContext = _databaseContextFactory.GetNewContext();
        try
        {
            databaseContext.Database.ExecuteSqlRaw(migrateDatabaseScript);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Could not migrate the database from {previousVersion} to {currentVersion}");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_DatabaseContext_Event",
                LogLevel.Critical,
                new DatabaseContextErrorEvent("MigratingDatabase", ex));
            return;
        }

        try
        {
            var migrationHandlers = _customMigrationHandlerFactory
                .GetCustomMigrationHandlers(previousVersion, currentVersion);

            // Call any custom migrations.
            foreach (var migration in migrationHandlers)
            {
                var shouldContinue = migration.PrepareForMigration();

                if (shouldContinue)
                {
                    migration.Migrate();
                }
                else
                {
                    Log.Warning($"Prepare for {nameof(migration)} returned false.  Not running Execute");
                }
            }

            schemaAccessor.WriteSchemaVersion(currentVersion);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Could not migrate the database from {previousVersion} to {currentVersion}");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_DatabaseContext_Event",
                LogLevel.Critical,
                new DatabaseContextErrorEvent("MigratingDatabase", ex));
        }
    }

    /// <summary>
    /// Reads the migration script and returns the contents.
    /// </summary>
    /// <param name="previousVersion">The version stored inside the schema file.</param>
    /// <param name="currentVersion">The version to migrate to.</param>
    /// <returns>The contents of the script file.  String.Empty if the file does not exist.</returns>
    public string GetMigrationQuery(uint previousVersion, uint currentVersion)
    {
        var queryFileLocation = $"Assets/MigrationScripts/{previousVersion}To{currentVersion}.sql";
        if (RuntimeHelper.IsMSIX)
        {
            Uri uri = new Uri($"ms-appx:///{queryFileLocation}");
            var migrationStorageFile = StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().Result;
            if (migrationStorageFile != null)
            {
                return FileIO.ReadTextAsync(migrationStorageFile).AsTask().Result.ToString();
            }
            else
            {
                Log.Warning($"Cound not find the migration script ms-appx:///{queryFileLocation}");
                return string.Empty;
            }
        }
        else
        {
            if (File.Exists($"{queryFileLocation}"))
            {
                return File.ReadAllText($"{queryFileLocation}");
            }
            else
            {
                Log.Warning($"Cound not find the migration script {queryFileLocation}");
                return string.Empty;
            }
        }
    }
}
