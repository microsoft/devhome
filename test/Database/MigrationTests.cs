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

[TestClass]
public class MigrationTests
{
    private readonly DatabaseTestHelper _databaseTestHelper = new();

    [TestInitialize]
    public void SetupTestAssets()
    {
        _databaseTestHelper.SetupTestAssets();
    }

    [TestCleanup]
    public void CleanupTestAssets()
    {
        _databaseTestHelper.RemoveTestAssets();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void TestShouldMigrateDatabase()
    {
        _databaseTestHelper.DatabaseContextfactory.Object.GetNewContext().Database.EnsureCreated();
        var migrator = _databaseTestHelper.MakeMigrator();

        // versions are the same.
        _databaseTestHelper.SetPreviousSchemaVersion(1);
        _databaseTestHelper.SetCurrentSchemaVersion(1);
        Assert.IsFalse(migrator.ShouldMigrateDatabase());

        // Migrate to a new version.
        _databaseTestHelper.SetPreviousSchemaVersion(1);
        _databaseTestHelper.SetCurrentSchemaVersion(2);
        Assert.IsTrue(migrator.ShouldMigrateDatabase());

        // Migrate to an older version
        // Moving to a lower version is no-opt because the old schema version
        // should have all the tables as the upgraded version.
        // a.k.a no tables were dropped going from v2->v3
        _databaseTestHelper.SetPreviousSchemaVersion(3);
        _databaseTestHelper.SetCurrentSchemaVersion(2);
        Assert.IsFalse(migrator.ShouldMigrateDatabase());

        // Test getting previous version from the user_version pragma
        _databaseTestHelper.SetPreviousSchemaVersion(0);
        _databaseTestHelper.SchemaAccessTester.DeleteFile();

        // Test with the same version
        var userVersionQuery = $"PRAGMA user_version = {2}";
        _databaseTestHelper.DatabaseContextfactory.Object.GetNewContext().Database.ExecuteSqlRaw(userVersionQuery);
        Assert.IsFalse(migrator.ShouldMigrateDatabase());

        // Test with a higher previous version.
        userVersionQuery = $"PRAGMA user_version = {3}";
        _databaseTestHelper.DatabaseContextfactory.Object.GetNewContext().Database.ExecuteSqlRaw(userVersionQuery);
        Assert.IsFalse(migrator.ShouldMigrateDatabase());

        // Test with a lower previous version.
        userVersionQuery = $"PRAGMA user_version = {1}";
        _databaseTestHelper.DatabaseContextfactory.Object.GetNewContext().Database.ExecuteSqlRaw(userVersionQuery);
        Assert.IsTrue(migrator.ShouldMigrateDatabase());

        // Always migrate if the database file does not exist.
        // Migrator will not use the file regardless if it exists.
        _databaseTestHelper.RemoveTestAssets();
        _databaseTestHelper.SchemaAccessTester.DeleteFile();

        // Test same version
        _databaseTestHelper.SetPreviousSchemaVersion(1);
        _databaseTestHelper.SetCurrentSchemaVersion(1);
        Assert.IsTrue(migrator.ShouldMigrateDatabase());

        // Test a lower previous version
        _databaseTestHelper.SetPreviousSchemaVersion(1);
        _databaseTestHelper.SetCurrentSchemaVersion(2);
        Assert.IsTrue(migrator.ShouldMigrateDatabase());

        // Test a higher previous version
        _databaseTestHelper.SetPreviousSchemaVersion(2);
        _databaseTestHelper.SetCurrentSchemaVersion(1);
        Assert.IsTrue(migrator.ShouldMigrateDatabase());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void TestMigrate0To1()
    {
        RemoveAndMigrateDatabase(0, 1);
        var tableNames = _databaseTestHelper.DatabaseContext.Object.Database
            .SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type='table'")
            .ToList();

        Assert.IsTrue(tableNames.Any(x => x.Equals("Repository", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void TestMigrateDatabaseWithNonExistentScript()
    {
        RemoveAndMigrateDatabase(0, 1);

        _databaseTestHelper.SetPreviousSchemaVersion(1);
        _databaseTestHelper.SetCurrentSchemaVersion(uint.MaxValue);
        _databaseTestHelper.MakeMigrator().MigrateDatabase();
        Assert.AreEqual(1U, _databaseTestHelper.SchemaAccessTester.GetPreviousSchemaVersion());
    }

    private void RemoveAndMigrateDatabase(uint previousVersion, uint currentVersion)
    {
        _databaseTestHelper.RemoveTestAssets();
        _databaseTestHelper.SetPreviousSchemaVersion(previousVersion);
        _databaseTestHelper.SetCurrentSchemaVersion(currentVersion);
        _databaseTestHelper.MakeMigrator().MigrateDatabase();
        Assert.IsFalse(_databaseTestHelper.DatabaseContextfactory.Object.GetNewContext().Database.EnsureCreated());
        Assert.AreEqual(currentVersion, _databaseTestHelper.SchemaAccessTester.GetPreviousSchemaVersion());
    }
}
