// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Database;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using Microsoft.EntityFrameworkCore;

namespace DevHome.Test.Database;

[TestClass]
public class RepositoryTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void ReadAndWriteRepositoryData()
    {
        var dbContext = new DevHomeDatabaseContext();

        // Reset the database
        // Not the best way to test.  I will change the test to a mock database
        // in the future.
        dbContext.ChangeTracker
            .Entries()
            .ToList()
            .ForEach(e => e.State = EntityState.Detached);

        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();

        // Insert a new record.
        var newRepo = new Repository();
        dbContext.Add(newRepo);
        dbContext.SaveChanges();

        var allRepositories = dbContext.Repositories.ToList();
        Assert.AreEqual(1, allRepositories.Count);

        var savedRepository = allRepositories[0];
        Assert.AreEqual(string.Empty, savedRepository.RepositoryName);
        Assert.AreEqual(string.Empty, savedRepository.RepositoryClonePath);
        Assert.IsTrue(savedRepository.CreatedUTCDate > DateTime.MinValue);
        Assert.AreEqual(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc), savedRepository.UpdatedUTCDate);

        // Modify the record.
        savedRepository.RepositoryName = "MyNewName";
        dbContext.SaveChanges();

        allRepositories = dbContext.Repositories.ToList();
        Assert.AreEqual(1, allRepositories.Count);

        savedRepository = allRepositories[0];
        Assert.AreEqual("MyNewName", savedRepository.RepositoryName);
        Assert.AreEqual(string.Empty, savedRepository.RepositoryClonePath);
        Assert.IsTrue(savedRepository.CreatedUTCDate > DateTime.MinValue);
        Assert.AreEqual(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc), savedRepository.UpdatedUTCDate);
    }
}
