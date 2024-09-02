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
    [DataRow("", FolderStatusProp, "Branch: main | +1 ~1 -0 | +0 ~7 -0")]
    [DataRow(".gitmodules", StatusProp, "Staged, Modified")]
    [DataRow("README.txt", StatusProp, "")]
    [DataRow("sm_added_and_uncommitted", StatusProp, "Staged")]
    [DataRow("sm_changed_file", StatusProp, "Submodule dirty")]
    [DataRow("sm_changed_head", StatusProp, "Submodule changed")]
    [DataRow("sm_changed_index", StatusProp, "Submodule dirty")]
    [DataRow("sm_changed_untracked_file", StatusProp, "Submodule dirty")]
    [DataRow("sm_missing_commits", StatusProp, "Submodule changed")]
    [DataRow("sm_missing_commits_detached", StatusProp, "Submodule changed")]
    [DataRow("sm_unchanged", StatusProp, "")]
    [DataRow("sm_unchanged_detached", StatusProp, "")]
    public void BaseRepoProperties(string path, string property, string value)
    {
        Assert.IsNotNull(_repo);
        var result = _repo.GetProperties([property], path);
        Assert.IsNotNull(result);
        Assert.AreEqual(value, result[property]);
    }
}
