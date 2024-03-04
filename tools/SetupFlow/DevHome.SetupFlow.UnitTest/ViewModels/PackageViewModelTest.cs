// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.UnitTest.Helpers;
using DevHome.SetupFlow.ViewModels;
using Moq;

namespace DevHome.SetupFlow.UnitTest.ViewModels;

[TestClass]
public class PackageViewModelTest : BaseSetupFlowTest
{
    private const string MockPackageUrl = "https://mock/packageUrl";
    private const string MockPublisherUrl = "https://mock/publisherUrl";
    private const string WinGetPkgsUrl = "https://github.com/microsoft/winget-pkgs";
    private const string MsStoreAppUrl = "ms-windows-store://pdp/?productid=mockId";

    [TestMethod]
    public void CreatePackageViewModel_Success()
    {
        var packageCatalog = PackageHelper.CreatePackageCatalog(10);
        var packageCatalogViewModel = TestHost!.CreateInstance<PackageCatalogViewModel>(packageCatalog);
        var expectedPackages = packageCatalog.Packages.ToList();
        var packages = packageCatalogViewModel.Packages.ToList();
        Assert.AreEqual(expectedPackages.Count, packages.Count);
        for (var i = 0; i < expectedPackages.Count; ++i)
        {
            Assert.AreEqual(expectedPackages[i].Name, packages[i].Name);
            Assert.AreEqual(expectedPackages[i].InstalledVersion, packages[i].InstalledVersion);
        }
    }

    [TestMethod]
    [DataRow(MockPackageUrl, MockPublisherUrl, MockPackageUrl)]
    [DataRow("", MockPublisherUrl, MockPublisherUrl)]
    [DataRow("", "", WinGetPkgsUrl)]
    public void LearnMore_PackageFromWinGetOrCustomCatalog_ReturnsExpectedUri(string packageUrl, string publisherUrl, string expectedUrl)
    {
        // Arrange
        WindowsPackageManager!.Setup(wpm => wpm.IsMsStorePackage(It.IsAny<IWinGetPackage>())).Returns(false);
        var package = PackageHelper.CreatePackage("mockId");
        package.Setup<Uri?>(p => p.PackageUrl).Returns(string.IsNullOrEmpty(packageUrl) ? null : new Uri(packageUrl));
        package.Setup<Uri?>(p => p.PublisherUrl).Returns(string.IsNullOrEmpty(publisherUrl) ? null : new Uri(publisherUrl));

        // Act
        var packageViewModel = TestHost!.CreateInstance<PackageViewModel>(package.Object);

        // Assert
        Assert.AreEqual(expectedUrl, packageViewModel.GetLearnMoreUri().ToString());
    }

    [TestMethod]
    [DataRow(MockPackageUrl, MockPublisherUrl, MockPackageUrl)]
    [DataRow("", MockPublisherUrl, MsStoreAppUrl)]
    [DataRow("", "", MsStoreAppUrl)]
    public void LearnMore_PackageFromMsStoreCatalog_ReturnsExpectedUri(string packageUrl, string publisherUrl, string expectedUrl)
    {
        // Arrange
        WindowsPackageManager!.Setup(wpm => wpm.IsMsStorePackage(It.IsAny<IWinGetPackage>())).Returns(true);
        var package = PackageHelper.CreatePackage("mockId");
        package.Setup<Uri?>(p => p.PackageUrl).Returns(string.IsNullOrEmpty(packageUrl) ? null : new Uri(packageUrl));
        package.Setup<Uri?>(p => p.PublisherUrl).Returns(string.IsNullOrEmpty(publisherUrl) ? null : new Uri(publisherUrl));

        // Act
        var packageViewModel = TestHost!.CreateInstance<PackageViewModel>(package.Object);

        // Assert
        Assert.AreEqual(expectedUrl, packageViewModel.GetLearnMoreUri().ToString());
    }

    [TestMethod]
    [DataRow("v1", "mockWinGet", "Microsoft", "v1 | mockWinGet | Microsoft")]
    [DataRow("v1", "mockWinGet", "", "v1 | mockWinGet")]
    [DataRow("Unknown", "mockMsStore", "Microsoft", "mockMsStore | Microsoft")]
    [DataRow("Unknown", "mockMsStore", "", "mockMsStore")]
    public void PackageDescription_VersionAndPublisherAreOptional_ReturnsExpectedDescription(
        string version,
        string source,
        string publisher,
        string expectedDescription)
    {
        // Arrange
        WindowsPackageManager.Setup(wpm => wpm.IsMsStorePackage(It.IsAny<IWinGetPackage>())).Returns(source == "mockMsStore");
        var package = PackageHelper.CreatePackage("mockId");
        package.Setup(p => p.CatalogId).Returns(source);
        package.Setup(p => p.CatalogName).Returns(source);
        package.Setup(p => p.PublisherName).Returns(publisher);
        package.Setup(p => p.IsInstalled).Returns(true);
        package.Setup(p => p.InstalledVersion).Returns(version);
        StringResource
            .Setup(sr => sr.GetLocalized(StringResourceKey.PackageDescriptionThreeParts, It.IsAny<object[]>()))
            .Returns((string key, object[] args) => $"{args[0]} | {args[1]} | {args[2]}");
        StringResource
            .Setup(sr => sr.GetLocalized(StringResourceKey.PackageDescriptionTwoParts, It.IsAny<object[]>()))
            .Returns((string key, object[] args) => $"{args[0]} | {args[1]}");

        // Act
        var packageViewModel = TestHost.CreateInstance<PackageViewModel>(package.Object);

        // Assert
        Assert.AreEqual(expectedDescription, packageViewModel.PackageFullDescription);
    }
}
