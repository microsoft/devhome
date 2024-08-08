// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using FileExplorerGitIntegration.Models;
using LibGit2Sharp;

namespace FileExplorerGitIntegration.UnitTest;

[TestClass]
public class GitLocalRepositoryProviderUnitTests
{
    private static string RepoPath => Path.Combine(Path.GetTempPath(), "GitTestRepository");

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        Debug.WriteLine("ClassInitialize");
        const string url = "http://github.com/libgit2/TestGitRepository";
        try
        {
            _ = Repository.Clone(url, RepoPath);
        }
        catch (NameConflictException)
        {
            // Clean stale test state and try again
            if (Directory.Exists(RepoPath))
            {
                // Cloning the repo leads to files that are hidden and readonly (such as under the .git directory).
                // Therefore, change the attribute so they can be deleted
                var repoDirectory = new DirectoryInfo(RepoPath)
                {
                    Attributes = System.IO.FileAttributes.Normal,
                };

                foreach (var dirInfo in repoDirectory.GetFileSystemInfos("*", SearchOption.AllDirectories))
                {
                    dirInfo.Attributes = System.IO.FileAttributes.Normal;
                }

                Directory.Delete(RepoPath, true);
            }

            _ = Repository.Clone(url, RepoPath);
        }
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        Debug.WriteLine("ClassCleanup");
        if (Directory.Exists(RepoPath))
        {
            // Cloning the repo leads to files that are hidden and readonly (such as under the .git directory).
            // Therefore, change the attribute so they can be deleted
            var repoDirectory = new DirectoryInfo(RepoPath)
            {
                Attributes = FileAttributes.Normal,
            };

            foreach (var dirInfo in repoDirectory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                dirInfo.Attributes = FileAttributes.Normal;
            }

            Directory.Delete(RepoPath, true);
        }
    }

    [TestMethod]
    public void OpenRepository()
    {
        _ = new GitLocalRepository(RepoPath);
        GitLocalRepositoryProviderFactory gitProvider = new GitLocalRepositoryProviderFactory();
        var result = gitProvider.GetRepository(RepoPath);
        Assert.IsNotNull(result.Repository);
    }

    [TestMethod]
    public void GetProperties()
    {
        var properties = new string[]
        {
            "System.VersionControl.LastChangeMessage",
            "System.VersionControl.LastChangeAuthorName",
            "System.VersionControl.LastChangeDate",
            "System.VersionControl.LastChangeAuthorEmail",
            "System.VersionControl.LastChangeID",
            "System.VersionControl.Status",
            "System.VersionControl.CurrentFolderStatus",
        };

        var relativePath = Path.Join("a", "a1");
        GitLocalRepository repo = new GitLocalRepository(RepoPath);
        var result = repo.GetProperties(properties, relativePath);
        Assert.IsNotNull(result);
        Assert.AreEqual(result["System.VersionControl.Status"], string.Empty);
        Assert.AreEqual(result["System.VersionControl.LastChangeDate"], new System.DateTimeOffset(2005, 4, 7, 15, 31, 13, new System.TimeSpan(-7, 0, 0)));
        Assert.AreEqual(result["System.VersionControl.LastChangeMessage"], "Fourth a/a1");
        Assert.AreEqual(result["System.VersionControl.LastChangeAuthorEmail"], "a.u.thor@example.com");
        Assert.AreEqual(result["System.VersionControl.LastChangeAuthorName"], "A U Thor");
        Assert.AreEqual(result["System.VersionControl.LastChangeID"], "d0114ab8ac326bab30e3a657a0397578c5a1af88");
        Assert.AreEqual(result["System.VersionControl.CurrentFolderStatus"], "Branch: master ≡ | +0 ~0 -0 | +0 ~0 -0");
    }

    [TestMethod]
    public void GitStatus()
    {
        const string repoStatusProperty = "System.VersionControl.CurrentFolderStatus";
        var properties = new string[]
        {
            repoStatusProperty,
        };
        var localRepo = new GitLocalRepository(RepoPath);
        var result = localRepo.GetProperties(properties, ".");
        Assert.AreEqual(result[repoStatusProperty], "Branch: master ≡ | +0 ~0 -0 | +0 ~0 -0");

        // Add a file
        var newFileName = "newfile.txt";
        var newFilePath = Path.Join(RepoPath, newFileName);
        File.WriteAllText(newFilePath, "Initial content");
        result = localRepo.GetProperties(properties, ".");
        Assert.AreEqual(result[repoStatusProperty], "Branch: master ≡ | +0 ~0 -0 | +1 ~0 -0");

        // Stage that add
        var modifiedRepo = new Repository(RepoPath);
        modifiedRepo.Index.Add(newFileName);
        modifiedRepo.Index.Write();
        result = localRepo.GetProperties(properties, ".");
        Assert.AreEqual(result[repoStatusProperty], "Branch: master ≡ | +1 ~0 -0 | +0 ~0 -0");

        // Re-modify the staged file
        File.WriteAllText(newFilePath, "New content");
        result = localRepo.GetProperties(properties, ".");
        Assert.AreEqual(result[repoStatusProperty], "Branch: master ≡ | +1 ~0 -0 | +0 ~1 -0");

        // Delete the file, the index still shows the add
        File.Delete(newFilePath);
        result = localRepo.GetProperties(properties, ".");
        Assert.AreEqual(result[repoStatusProperty], "Branch: master ≡ | +1 ~0 -0 | +0 ~0 -1");

        // Remove from index, back to clean state
        modifiedRepo.Index.Remove(newFileName);
        modifiedRepo.Index.Write();
        result = localRepo.GetProperties(properties, ".");
        Assert.AreEqual(result[repoStatusProperty], "Branch: master ≡ | +0 ~0 -0 | +0 ~0 -0");
    }

    [TestMethod]
    public void DetectFileRename()
    {
        const string statusProperty = "System.VersionControl.Status";
        var properties = new string[]
        {
            statusProperty,
        };
        var localRepo = new GitLocalRepository(RepoPath);
        var relativeFromPath = Path.Join("a", "a1");
        var relativeToPath = Path.Join("a", "a1_renamed");

        var result = localRepo.GetProperties(properties, relativeFromPath);
        Assert.AreEqual(result[statusProperty], string.Empty);
        result = localRepo.GetProperties(properties, relativeToPath);
        Assert.IsNull(result[statusProperty]);

        var modifiedRepo = new Repository(RepoPath);
        Commands.Move(modifiedRepo, relativeFromPath, relativeToPath);
        Commands.Stage(modifiedRepo, relativeFromPath);
        Commands.Stage(modifiedRepo, relativeToPath);
        result = localRepo.GetProperties(properties, relativeFromPath);
        Assert.IsNull(result[statusProperty]);
        result = localRepo.GetProperties(properties, relativeToPath);
        Assert.AreEqual(result[statusProperty], "Staged rename");

        modifiedRepo.Reset(ResetMode.Hard);
        result = localRepo.GetProperties(properties, relativeFromPath);
        Assert.AreEqual(result[statusProperty], string.Empty);
        result = localRepo.GetProperties(properties, relativeToPath);
        Assert.IsNull(result[statusProperty]);
    }
}
