// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Helpers;
using DevHome.Database;
using DevHome.Database.Factories;
using DevHome.Database.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Windows.Storage;

namespace DevHome.Test.Database;

internal sealed class DatabaseTestHelper
{
    private const string DatabaseName = "TestDevHomeDatabase.db";

    public SchemaAccessTester SchemaAccessTester { get; }

    public Mock<ISchemaAccessFactory> SchemaAccessorFactory { get; }

    public Mock<IDevHomeDatabaseContext> DatabaseContext { get; }

    public Mock<IDevHomeDatabaseContextFactory> DatabaseContextfactory { get; }

    private string _dbPath = string.Empty;

    internal DatabaseTestHelper()
    {
        SchemaAccessTester = new SchemaAccessTester();

        SchemaAccessorFactory = new Mock<ISchemaAccessFactory>();

        DatabaseContext = new Mock<IDevHomeDatabaseContext>();

        DatabaseContextfactory = new Mock<IDevHomeDatabaseContextFactory>();
    }

    public void SetupTestAssets()
    {
        if (RuntimeHelper.IsMSIX)
        {
            _dbPath = Path.Join(ApplicationData.Current.LocalFolder.Path, DatabaseName);
        }
        else
        {
            _dbPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DatabaseName);
        }

        RemoveTestAssets();
        SchemaAccessTester.DeleteFile();

        SchemaAccessorFactory.Setup(x => x.GenerateSchemaAccessor()).Returns(SchemaAccessTester);
        DatabaseContext.Setup(x => x.SchemaVersion).Returns(0);
        DatabaseContext.Setup(x => x.Database).Returns(new DevHomeDatabaseContext(_dbPath).Database);

        DatabaseContextfactory.Setup(x => x.GetNewContext()).Returns(DatabaseContext.Object);
    }

    public void CleanupTestAssets()
    {
        RemoveTestAssets();
        SchemaAccessTester.DeleteFile();
    }

    public void SetPreviousSchemaVersion(uint newVersion)
    {
        SchemaAccessTester.Version = newVersion;
        SchemaAccessTester.WriteSchemaVersion(newVersion);
    }

    public void SetCurrentSchemaVersion(uint newVersion)
    {
        DatabaseContext.Setup(x => x.SchemaVersion).Returns(newVersion);
    }

    public void RemoveTestAssets()
    {
        var dbContext = new DevHomeDatabaseContext(_dbPath);

        // Reset the database
        dbContext.ChangeTracker
            .Entries()
            .ToList()
            .ForEach(e => e.State = EntityState.Detached);

        dbContext.Database.EnsureDeleted();
    }

    public DatabaseMigrationService MakeMigrator()
    {
        return new DatabaseMigrationService(
            SchemaAccessorFactory.Object,
            DatabaseContextfactory.Object,
            new CustomMigrationHandlerFactory());
    }
}
