// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Globalization;
using DevHome.Contracts.Services;
using DevHome.Services;

namespace DevHome.Test;

[TestClass]
public class GitWatcherTests
{
    private const string TestDirectory = @"C:\GitWatcherTest\";

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        if (Directory.Exists(TestDirectory))
        {
            Directory.Delete(TestDirectory, true);
        }

        GitWatcher.Instance.SetMonitoredSources(null, false);
    }

    [TestInitialize]
    public void TestInitialize()
    {
        if (Directory.Exists(TestDirectory))
        {
            Directory.Delete(TestDirectory, true);
        }

        Directory.CreateDirectory(TestDirectory);
        GitWatcher.Instance.SetMonitoredSources(new Collection<string> { TestDirectory }, false);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(TestDirectory))
        {
            Directory.Delete(TestDirectory, true);
        }
    }

    // [TestMethod]
    public void Synthetic_CreateDeleteRepo_Success()
    {
        var newRepoEventFired = new ManualResetEvent(false);
        var repoDeletedEventFired = new ManualResetEvent(false);

        GitRepositoryChangedEventArgs newResult = new(GitRepositoryChangeType.Deleted, "Invalid");
        GitRepositoryChangedEventArgs deletedResult = new(GitRepositoryChangeType.Created, "Invalid");

        GitWatcher.Instance.GitRepositoryCreated += (_, e) =>
        {
            newResult = e;
            newRepoEventFired.Set();
        };

        GitWatcher.Instance.GitRepositoryDeleted += (_, e) =>
        {
            deletedResult = e;
            repoDeletedEventFired.Set();
        };

        const string TestRepo = "SyntheticRepo_CreateDeleteRepo_Success";
        const string CreationSentinelFile = "description";
        const string DeletionSentinelFile = "HEAD";
        var testRepoPath = Path.Combine(TestDirectory, TestRepo);
        var pathToGitFolder = Path.Combine(testRepoPath, ".git");

        Directory.CreateDirectory(pathToGitFolder);
        var tempFile = File.Create(Path.Combine(pathToGitFolder, DeletionSentinelFile));
        tempFile.Close();

        // Ensure event hasn't fired yet
        Assert.IsFalse(newRepoEventFired.WaitOne(0));

        // Create creation sentinel file and check for event firing
        tempFile = File.Create(Path.Combine(pathToGitFolder, CreationSentinelFile));
        tempFile.Close();
        Assert.IsTrue(newRepoEventFired.WaitOne(1000));
        newRepoEventFired.Reset();

        // Verify event details are correct
        Assert.AreEqual(GitRepositoryChangeType.Created, newResult.ChangeType);
        Assert.AreEqual(testRepoPath, newResult.RepositoryPath, true, CultureInfo.InvariantCulture);

        // Ensure deletion event hasn't fired
        Assert.IsFalse(repoDeletedEventFired.WaitOne(0));

        // Trigger deletion event
        Directory.Delete(testRepoPath, true);
        Assert.IsTrue(repoDeletedEventFired.WaitOne(1000));
        repoDeletedEventFired.Reset();

        // Verify deletion event details are correct
        Assert.AreEqual(GitRepositoryChangeType.Deleted, deletedResult.ChangeType);
        Assert.AreEqual(testRepoPath, deletedResult.RepositoryPath, true, CultureInfo.InvariantCulture);

        // Ensure creation event hasn't fired
        Assert.IsFalse(newRepoEventFired.WaitOne(0));
    }

    // [TestMethod]
    public void SyntheticRepo_CreateModifyDeleteFile_Success()
    {
        // TestRepo2 will test repositories created before the watcher, while TestRepo3 will test repositories
        // created after the watcher.
        const string TestRepo2 = "SyntheticRepo_CreateModifyDeleteFile_Success__PreWatcher";
        const string TestRepo3 = "SyntheticRepo_CreateModifyDeleteFile_Success__PostWatcher";
        const string CreationSentinelFile = "description";
        const string DeletionSentinelFile = "HEAD";
        const string WatcherTargetFile = "testfile.cfg";
        var testRepoPath2 = Path.Combine(TestDirectory, TestRepo2);
        var pathToGitFolder2 = Path.Combine(testRepoPath2, ".git");
        var pathToTargetFile2 = Path.Combine(testRepoPath2, WatcherTargetFile);
        var testRepoPath3 = Path.Combine(TestDirectory, TestRepo3);
        var pathToGitFolder3 = Path.Combine(testRepoPath3, ".git");
        var pathToTargetFile3 = Path.Combine(testRepoPath3, WatcherTargetFile);

        GitFileChangedEventArgs eventArgs = new(GitFileChangeType.Deleted, "Invalid", "Invalid");
        var eventFired = new ManualResetEvent(false);

        Directory.CreateDirectory(pathToGitFolder2);
        var tempFile = File.Create(Path.Combine(pathToGitFolder2, DeletionSentinelFile));
        tempFile.Close();

        tempFile = File.Create(Path.Combine(pathToGitFolder2, CreationSentinelFile));
        tempFile.Close();

        var watcher = GitWatcher.Instance.CreateFileWatcher(WatcherTargetFile);
        EventHandler<GitFileChangedEventArgs> eventHandler = (object? sender, GitFileChangedEventArgs args) =>
        {
            eventArgs = args;
            eventFired.Set();
        };

        watcher.FileCreated += eventHandler;
        watcher.FileModified += eventHandler;
        watcher.FileDeleted += eventHandler;

        Directory.CreateDirectory(pathToGitFolder3);
        tempFile = File.Create(Path.Combine(pathToGitFolder3, DeletionSentinelFile));
        tempFile.Close();

        tempFile = File.Create(Path.Combine(pathToGitFolder3, CreationSentinelFile));
        tempFile.Close();

        // Verify watcher hasn't fired yet
        Assert.IsFalse(eventFired.WaitOne(0));

        // TESTS FOR REPO 2

        // Test file creation
        var testFile2 = File.Create(pathToTargetFile2);
        testFile2.Close();
        Assert.IsTrue(eventFired.WaitOne(1000));
        eventFired.Reset();

        Assert.AreEqual(GitFileChangeType.Created, eventArgs.ChangeType);
        Assert.AreEqual(testRepoPath2, eventArgs.RepositoryPath, true, CultureInfo.InvariantCulture);
        Assert.AreEqual(pathToTargetFile2, eventArgs.FilePath, true, CultureInfo.InvariantCulture);

        // Test file modification
        testFile2 = File.OpenWrite(pathToTargetFile2);
        testFile2.Write([1, 2, 3, 4]);
        testFile2.Close();
        Assert.IsTrue(eventFired.WaitOne(1000));
        eventFired.Reset();

        Assert.AreEqual(GitFileChangeType.Modified, eventArgs.ChangeType);
        Assert.AreEqual(testRepoPath2, eventArgs.RepositoryPath, true, CultureInfo.InvariantCulture);
        Assert.AreEqual(pathToTargetFile2, eventArgs.FilePath, true, CultureInfo.InvariantCulture);

        // Test file deletion
        File.Delete(pathToTargetFile2);
        Assert.IsTrue(eventFired.WaitOne(1000));
        eventFired.Reset();

        Assert.AreEqual(GitFileChangeType.Deleted, eventArgs.ChangeType);
        Assert.AreEqual(testRepoPath2, eventArgs.RepositoryPath, true, CultureInfo.InvariantCulture);
        Assert.AreEqual(pathToTargetFile2, eventArgs.FilePath, true, CultureInfo.InvariantCulture);

        // TESTS FOR REPO 3

        // Test file creation
        var testFile3 = File.Create(pathToTargetFile3);
        testFile3.Close();
        Assert.IsTrue(eventFired.WaitOne(1000));
        eventFired.Reset();

        Assert.AreEqual(GitFileChangeType.Created, eventArgs.ChangeType);
        Assert.AreEqual(testRepoPath3, eventArgs.RepositoryPath, true, CultureInfo.InvariantCulture);
        Assert.AreEqual(pathToTargetFile3, eventArgs.FilePath, true, CultureInfo.InvariantCulture);

        // Test file modification
        testFile3 = File.OpenWrite(pathToTargetFile3);
        testFile3.Write([1, 2, 3, 4]);
        testFile3.Close();
        Assert.IsTrue(eventFired.WaitOne(1000));
        eventFired.Reset();

        Assert.AreEqual(GitFileChangeType.Modified, eventArgs.ChangeType);
        Assert.AreEqual(testRepoPath3, eventArgs.RepositoryPath, true, CultureInfo.InvariantCulture);
        Assert.AreEqual(pathToTargetFile3, eventArgs.FilePath, true, CultureInfo.InvariantCulture);

        // Test file deletion
        File.Delete(pathToTargetFile3);
        Assert.IsTrue(eventFired.WaitOne(1000));
        eventFired.Reset();

        Assert.AreEqual(GitFileChangeType.Deleted, eventArgs.ChangeType);
        Assert.AreEqual(testRepoPath3, eventArgs.RepositoryPath, true, CultureInfo.InvariantCulture);
        Assert.AreEqual(pathToTargetFile3, eventArgs.FilePath, true, CultureInfo.InvariantCulture);
    }
}
