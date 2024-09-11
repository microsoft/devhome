// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FileExplorerGitIntegration.Models;

namespace FileExplorerGitIntegration.UnitTest;

[TestClass]
public class WslParsingUnitTests
{
    [TestMethod]
    [DataRow("\\wsl$\\Ubuntu-20.04\\home\\user\\repo")]
    [DataRow("\\wsl.localhost\\Ubuntu-20.04\\home\\user\\repo")]
    [DataRow("\\wsl$\\Ubuntu\\home\\user\\repo")]
    [DataRow("\\wsl.localhost\\Ubuntu\\home\\user\\repo")]
    [DataRow("\\wsl.localhost\\Debian\\home\\user\\repo")]
    [DataRow("\\wsl$\\kali-linux\\home\\user\\repo")]
    [DataRow("\\wsl$\\Ubuntu-18.04\\home\\user\\testRepo")]
    [DataRow("\\wsl.localhost\\Ubuntu-18.04\\home\\user\\testRepo")]
    public void IsWSLRepoPositiveTests(string repositoryPath)
    {
        Assert.IsTrue(WslIntegrator.IsWSLRepo(repositoryPath));
    }

    [TestMethod]
    [DataRow("C:\\Users\\foo\\bar")]
    [DataRow("\\wsl$*\\Ubuntu\\home\\user\\repo")]
    [DataRow("D:\\wsl.localhost\\Ubuntu-20.04\\home\\user\\repo")]
    [DataRow("\\wsl.test\\Ubuntu\\home\\user\\repo")]
    public void IsWslRepoNegativeTests(string repositoryPath)
    {
        Assert.IsFalse(WslIntegrator.IsWSLRepo(repositoryPath));
    }

    [TestMethod]
    [DataRow("\\wsl$\\Ubuntu-20.04\\home\\user\\repo", "Ubuntu-20.04")]
    [DataRow("\\wsl.localhost\\Ubuntu-20.04\\home\\user\\repo", "Ubuntu-20.04")]
    [DataRow("\\wsl$\\Debian\\home\\user\\repo", "Debian")]
    [DataRow("\\wsl.localhost\\kali-linux\\home\\user\\repo", "kali-linux")]
    [DataRow("\\wsl.localhost\\UbuntuTest\\home\\user\\testRepo", "")]
    [DataRow("\\wsl$\\InvalidDistribution\\home\\user\\testRepo", "")]
    public void GetDistributionName(string repositoryPath, string value)
    {
        var distributionName = WslIntegrator.GetWslDistributionName(repositoryPath);
        Assert.AreEqual(value, distributionName);
    }

    [TestMethod]
    [DataRow("\\wsl$\\Ubuntu-20.04\\home\\user\\repo", "cd /home/user/repo && git ")]
    [DataRow("\\wsl.localhost\\Ubuntu-20.04\\home\\user\\repo", "cd /home/user/repo && git ")]
    [DataRow("\\wsl$\\Debian\\home\\user\\repo", "cd /home/user/repo && git ")]
    [DataRow("\\wsl.localhost\\kali-linux\\home\\user\\repo", "cd /home/user/repo && git ")]
    [DataRow("C:\\Users\\foo\\bar", "")]
    [DataRow("\\wsl$\\Ubuntu-18.04\\home\\user\\testRepo", "cd /home/user/testRepo && git ")]
    [DataRow("\\wsl.localhost\\Ubuntu-18.04\\home\\user\\testRepo", "cd /home/user/testRepo && git ")]
    public void GetArgumentPrefixForWslTest(string repositoryPath, string value)
    {
        var prefix = WslIntegrator.GetArgumentPrefixForWsl(repositoryPath);
        Assert.AreEqual(value, prefix);
    }
}
