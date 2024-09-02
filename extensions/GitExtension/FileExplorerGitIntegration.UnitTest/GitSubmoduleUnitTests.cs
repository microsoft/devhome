// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FileExplorerGitIntegration.Models;

namespace FileExplorerGitIntegration.UnitTest;

[TestClass]
public class GitSubmoduleUnitTests
{
    private const string FolderStatusProp = "System.VersionControl.CurrentFolderStatus";
    private const string StatusProp = "System.VersionControl.Status";

    private static SandboxHelper? _sandbox;
    private static GitLocalRepository? _repo;

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext)
    {
        _sandbox = new();
        var repopath = _sandbox.CreateSandbox("submodules");
        _sandbox.CreateSandbox("submodules_target");
        _repo = new GitLocalRepository(repopath);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        if (_sandbox is not null)
        {
            _sandbox.Cleanup();
            _sandbox = null;
        }

        _repo = null;
    }

    [TestMethod]
    [DataRow("", FolderStatusProp, "Branch: main | +0 ~0 -0 | +0 ~0 -0")]
    [DataRow(".gitmodules", StatusProp, "")]
    [DataRow("README.txt", StatusProp, "")]
    [DataRow("sm_unchanged", FolderStatusProp, "Branch: main | +0 ~0 -0 | +0 ~0 -0")]
    [DataRow("sm_unchanged", StatusProp, "")]
    [DataRow("sm_unchanged_detached", FolderStatusProp, "Branch: main | +0 ~0 -0 | +0 ~0 -0")]
    [DataRow("sm_unchanged", StatusProp, "")]
    public void BaseRepoProperties(string path, string property, string value)
    {
        Assert.IsNotNull(_repo);
        var result = _repo.GetProperties([property], path);
        Assert.IsNotNull(result);
        Assert.AreEqual(value, result[property]);
    }
}
