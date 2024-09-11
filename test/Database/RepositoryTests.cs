// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Database;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.SetupFlow.Views;
using Microsoft.EntityFrameworkCore;

namespace DevHome.Test.Database;

[TestClass]
public class RepositoryTests
{
    private const string ConfigurationFileLocation = @"The\Best\Configuration\Location";

    private const string CloneLocation = @"The\Best\File\Location";

    private const string RepositoryName = "DevHome";

    private const string RepositoryUri = "https://www.github.com/microsoft/devhome";

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
        dbContext.Database.EnsureCreated();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MakeAndReadDefaultRepositoryValues()
    {
        var dbContext = new DevHomeDatabaseContext();

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
        Assert.IsFalse(savedRepository.IsHidden);
        Assert.IsFalse(savedRepository.HasAConfigurationFile);
        Assert.AreEqual(string.Empty, savedRepository.RepositoryUri);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MakefilledInRepository()
    {
        var newRepo = new Repository();
        newRepo.ConfigurationFileLocation = ConfigurationFileLocation;
        newRepo.RepositoryClonePath = CloneLocation;
        newRepo.RepositoryName = RepositoryName;
        newRepo.IsHidden = true;
        newRepo.RepositoryUri = RepositoryUri;

        var dbContext = new DevHomeDatabaseContext();
        dbContext.Add(newRepo);
        dbContext.SaveChanges();

        var allRepositories = dbContext.Repositories.ToList();
        Assert.AreEqual(1, allRepositories.Count);

        var savedRepository = allRepositories[0];

        Assert.AreEqual(RepositoryName, savedRepository.RepositoryName);
        Assert.AreEqual(CloneLocation, savedRepository.RepositoryClonePath);
        Assert.IsTrue(savedRepository.CreatedUTCDate > DateTime.MinValue);
        Assert.AreEqual(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc), savedRepository.UpdatedUTCDate);
        Assert.IsTrue(savedRepository.IsHidden);
        Assert.IsTrue(savedRepository.HasAConfigurationFile);
        Assert.AreEqual(RepositoryUri, savedRepository.RepositoryUri);
    }
}
