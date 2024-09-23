// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Customization.ViewModels;
using DevHome.Database;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.Database.Factories;
using DevHome.Database.Services;
using DevHome.RepositoryManagement.Services;
using DevHome.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Moq;

namespace DevHome.Test.Database;

// TODO: Add Database tests.
[TestClass]
public class RepositoryTests
{
    private const string CloneLocation = @"TestSource\repos";

    private const string RepositoryName = "DevHome";

    private const string ConfigurationFileLocation = @".configurations\configuration.dsc.yaml";

    private const string RepositoryUri = "https://www.github.com/microsoft/devhome";

    private readonly string _repositoryCloneLocation = Path.Join(CloneLocation, RepositoryName);

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

        if (Directory.Exists(_repositoryCloneLocation))
        {
            // Cumbersome, but needed to remove read-only files.
            foreach (var repositoryFile in Directory.EnumerateFiles(_repositoryCloneLocation, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(repositoryFile, FileAttributes.Normal);
                File.Delete(repositoryFile);
            }

            foreach (var repositoryDirectory in Directory.GetDirectories(_repositoryCloneLocation, "*", SearchOption.AllDirectories).Reverse())
            {
                Directory.Delete(repositoryDirectory);
            }

            File.SetAttributes(_repositoryCloneLocation, FileAttributes.Normal);
            Directory.Delete(_repositoryCloneLocation, false);
        }

        LibGit2Sharp.Repository.Clone(RepositoryUri, _repositoryCloneLocation);
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
        Assert.IsTrue(savedRepository.UpdatedUTCDate > DateTime.MinValue);
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
        Assert.IsTrue(savedRepository.UpdatedUTCDate > DateTime.MinValue);
        Assert.IsTrue(savedRepository.IsHidden);
        Assert.IsTrue(savedRepository.HasAConfigurationFile);
        Assert.AreEqual(RepositoryUri, savedRepository.RepositoryUri);
        Assert.IsTrue(savedRepository.HasAConfigurationFile);
    }
}
