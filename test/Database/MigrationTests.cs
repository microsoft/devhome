// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Database;
using DevHome.Database.Factories;
using DevHome.Database.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DevHome.Test.Database;

[TestClass]
public class MigrationTests
{
    [TestInitialize]
    public void ResetDatabase()
    {
        var dbContext = new DevHomeDatabaseContext();

        // Reset the database
        // TODO: Do not test on a production database.
        dbContext.ChangeTracker
            .Entries()
            .ToList()
            .ForEach(e => e.State = EntityState.Detached);

        dbContext.Database.EnsureDeleted();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void TestShouldMigrateDatabase()
    {
        var schemaAccessor = new Mock<ISchemaAccessor>();
        schemaAccessor.Setup(x => x.GetPreviousSchemaVersion()).Returns(0);

        var schemaAccessorFactory = new Mock<ISchemaAccessFactory>();
        schemaAccessorFactory.Setup(x => x.GenerateSchemaAccessor()).Returns(schemaAccessor.Object);

        var databaseContext = new Mock<IDevHomeDatabaseContext>();
        databaseContext.Setup(x => x.SchemaVersion).Returns(1);

        var databaseContextfactory = new Mock<IDevHomeDatabaseContextFactory>();
        databaseContextfactory.Setup(x => x.GetNewContext()).Returns(databaseContext.Object);

        var migrator = new DatabaseMigrationService(
            schemaAccessorFactory.Object,
            databaseContextfactory.Object,
            new CustomMigrationHandlerFactory());

        Assert.IsTrue(migrator.ShouldMigrateDatabase());

        schemaAccessor.Setup(x => x.GetPreviousSchemaVersion()).Returns(1);

        Assert.IsFalse(migrator.ShouldMigrateDatabase());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void TestMigrateDatabase()
    {
        // Setup so migration will not occur.
        var schemaAccessor = new Mock<ISchemaAccessor>();
        schemaAccessor.Setup(x => x.GetPreviousSchemaVersion()).Returns(1);

        var schemaAccessorFactory = new Mock<ISchemaAccessFactory>();
        schemaAccessorFactory.Setup(x => x.GenerateSchemaAccessor()).Returns(schemaAccessor.Object);

        var databaseContext = new Mock<IDevHomeDatabaseContext>();
        databaseContext.Setup(x => x.SchemaVersion).Returns(1);

        var databaseContextfactory = new Mock<IDevHomeDatabaseContextFactory>();
        databaseContextfactory.Setup(x => x.GetNewContext()).Returns(databaseContext.Object);

        var migrator = new DatabaseMigrationService(
            schemaAccessorFactory.Object,
            databaseContextfactory.Object,
            new CustomMigrationHandlerFactory());

        migrator.MigrateDatabase();

        var dbContext = new DevHomeDatabaseContext();
        Assert.IsFalse(dbContext.Database.EnsureDeleted());
    }
}
