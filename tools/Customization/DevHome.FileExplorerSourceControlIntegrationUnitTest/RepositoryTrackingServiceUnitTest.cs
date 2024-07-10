// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using DevHome.FileExplorerSourceControlIntegration.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevHome.FileExplorerSourceControlIntegrationUnitTest;

[TestClass]
public class RepositoryTrackingServiceUnitTest
{
    private RepositoryTracking RepoTracker { get; set; } = new(Path.Combine(Path.GetTempPath()));

    private readonly string extension = "testExtension";

    private readonly string rootPath = "c:\\test\\rootPath";

    private readonly string caseAlteredRootPath = "C:\\TEST\\ROOTPATH";

    [TestMethod]
    public void AddRepository()
    {
        RepoTracker.AddRepositoryPath(extension, rootPath);
        var result = RepoTracker.GetAllTrackedRepositories();
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result.ContainsKey(rootPath));
        Assert.IsTrue(result.ContainsValue(extension));
        RepoTracker.RemoveRepositoryPath(rootPath);
    }

    [TestMethod]
    public void RemoveRepository()
    {
        RepoTracker.AddRepositoryPath(extension, rootPath);
        var result = RepoTracker.GetAllTrackedRepositories();
        Assert.AreEqual(1, result.Count);
        RepoTracker.RemoveRepositoryPath(rootPath);
        result = RepoTracker.GetAllTrackedRepositories();
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void GetAllRepositories()
    {
        for (var i = 0; i < 5; i++)
        {
            RepoTracker.AddRepositoryPath(extension, string.Concat(rootPath, i));
        }

        var result = RepoTracker.GetAllTrackedRepositories();
        Assert.IsNotNull(result);
        Assert.AreEqual(5, result.Count);
        Assert.IsTrue(result.ContainsKey(string.Concat(rootPath, 0)));
        Assert.IsTrue(result.ContainsValue(extension));

        for (var i = 0; i < 5; i++)
        {
            RepoTracker.RemoveRepositoryPath(string.Concat(rootPath, i));
        }
    }

    [TestMethod]
    public void GetAllRepositories_Empty()
    {
        var result = RepoTracker.GetAllTrackedRepositories();
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void GetSourceControlProviderFromRepositoryPath()
    {
        RepoTracker.AddRepositoryPath(extension, rootPath);
        var result = RepoTracker.GetSourceControlProviderForRootPath(rootPath);
        Assert.IsNotNull(result);
        Assert.AreEqual(extension, result);
        RepoTracker.RemoveRepositoryPath(rootPath);
    }

    [TestMethod]
    public void AddRepository_DoesNotAddDuplicateValues()
    {
        RepoTracker.AddRepositoryPath(extension, rootPath);

        // Atempt to add duplicate entry that is altered in case
        RepoTracker.AddRepositoryPath(extension, caseAlteredRootPath);

        // Ensure duplicate is not added and count is 1
        var result = RepoTracker.GetAllTrackedRepositories();
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result.ContainsKey(rootPath));
        Assert.IsTrue(result.ContainsValue(extension));

        RepoTracker.RemoveRepositoryPath(rootPath);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (File.Exists(Path.Combine(Path.GetTempPath(), "TrackedRepositoryStore.json")))
        {
            File.Delete(Path.Combine(Path.GetTempPath(), "TrackedRepositoryStore.json"));
        }
    }
}
