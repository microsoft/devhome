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
        Assert.AreEqual(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc), savedRepository.CreatedUTCDate);
        Assert.AreEqual(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc), savedRepository.UpdatedUTCDate);
        Assert.IsNull(savedRepository.RepositoryMetadata);
    }
}
