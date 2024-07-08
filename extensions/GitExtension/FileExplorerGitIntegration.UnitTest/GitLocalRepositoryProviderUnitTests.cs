// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;
using FileExplorerGitIntegration.Models;
using LibGit2Sharp;
using Microsoft.Win32;

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

        var relativePath = "a\\a1";
        GitLocalRepository repo = new GitLocalRepository(RepoPath);
        var result = repo.GetProperties(properties, relativePath);
        Assert.IsNotNull(result);
        Assert.AreEqual(result["System.VersionControl.Status"], string.Empty);
        Assert.AreEqual(result["System.VersionControl.LastChangeDate"], new System.DateTimeOffset(2005, 4, 7, 15, 31, 13, new System.TimeSpan(-7, 0, 0)));
        Assert.AreEqual(result["System.VersionControl.LastChangeMessage"], "Fourth a/a1");
        Assert.AreEqual(result["System.VersionControl.LastChangeAuthorEmail"], "a.u.thor@example.com");
        Assert.AreEqual(result["System.VersionControl.LastChangeAuthorName"], "A U Thor");
        Assert.AreEqual(result["System.VersionControl.LastChangeID"], "d0114ab8ac326bab30e3a657a0397578c5a1af88");
        Assert.AreEqual(result["System.VersionControl.CurrentFolderStatus"], "master AheadBy: 0 BehindBy: 0");
    }

    [TestMethod]
    public void DetectAndInvokeGit()
    {
        Assert.IsTrue(IsGitInstalled());
        PowerShell git = PowerShell.Create();
        git.AddScript("cd /");
        git.AddScript($"cd {RepoPath}");
        git.AddScript("git status");
        var output = git.Invoke();
        var branchName = ParseBranchName(output);
        Assert.IsNotNull(branchName);
        Assert.AreEqual(branchName, "master");
    }

    private string? ParseBranchName(Collection<PSObject> output)
    {
        string? branchName = null;
        foreach (var line in output)
        {
            if (line.BaseObject is string outputLine && outputLine.StartsWith("On branch ", StringComparison.Ordinal))
            {
                branchName = outputLine.Substring("On branch ".Length);
                break;
            }
        }

        return branchName;
    }

    private bool IsGitInstalled()
    {
        // const string registryKeyPath = @"HKEY_CURRENT_USER\Console\Git CMD";
        using var registryKey = Registry.CurrentUser.OpenSubKey("Console");
        var isInstalled = registryKey?.OpenSubKey("Git CMD") != null;
        registryKey!.Close();
        return isInstalled;
    }
}
