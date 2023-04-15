// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Extensions;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.UnitTest.Helpers;
using Microsoft.Management.Deployment;
using Moq;

namespace DevHome.SetupFlow.UnitTest.ViewModels;

[TestClass]
public class WinGetPackageJsonDataSourceTest : BaseSetupFlowTest
{
    [TestMethod]
    public void LoadCatalogs_Success_ReturnsWinGetCatalogs()
    {
        // Prepare expected package
        var expectedPackages = new List<IWinGetPackage>
        {
            PackageHelper.CreatePackage("mock1").Object,
            PackageHelper.CreatePackage("mock2").Object,
        };
        ConfigureWinGetCatalogPackages(expectedPackages);

        // Act
        var loadedPackages = LoadCatalogsFromJsonDataSource("AppManagementPackages_Success.json");

        // Assert
        Assert.AreEqual(1, loadedPackages.Count);
        Assert.AreEqual("mockTitle_1", loadedPackages[0].Name);
        Assert.AreEqual("mockDescription_1", loadedPackages[0].Description);
        Assert.AreEqual(expectedPackages.Count, loadedPackages[0].Packages.Count);
        Assert.AreEqual(expectedPackages[0].Id, loadedPackages[0].Packages.ElementAt(0).Id);
        Assert.AreEqual(expectedPackages[1].Id, loadedPackages[0].Packages.ElementAt(1).Id);
    }

    [TestMethod]
    public void LoadCatalogs_OrderedPackages_ReturnsWinGetCatalogsWithMatchingInputOrder()
    {
        // Prepare expected package
        var expectedPackages = new List<IWinGetPackage>
        {
            PackageHelper.CreatePackage("mock1").Object,
            PackageHelper.CreatePackage("mock2").Object,
        };
        ConfigureWinGetCatalogPackages(expectedPackages);

        // Act
        var loadedPackages = LoadCatalogsFromJsonDataSource("AppManagementPackages_Order.json");

        // Assert
        Assert.AreEqual(2, loadedPackages.Count);

        Assert.AreEqual("mockTitle_1", loadedPackages[0].Name);
        Assert.AreEqual("mockDescription_1", loadedPackages[0].Description);
        Assert.AreEqual(expectedPackages.Count, loadedPackages[0].Packages.Count);
        Assert.AreEqual(expectedPackages[0].Id, loadedPackages[0].Packages.ElementAt(0).Id);
        Assert.AreEqual(expectedPackages[1].Id, loadedPackages[0].Packages.ElementAt(1).Id);

        Assert.AreEqual("mockTitle_2", loadedPackages[1].Name);
        Assert.AreEqual("mockDescription_2", loadedPackages[1].Description);
        Assert.AreEqual(expectedPackages.Count, loadedPackages[1].Packages.Count);
        Assert.AreEqual(expectedPackages[1].Id, loadedPackages[1].Packages.ElementAt(0).Id);
        Assert.AreEqual(expectedPackages[0].Id, loadedPackages[1].Packages.ElementAt(1).Id);
    }

    [TestMethod]
    public void LoadCatalogs_EmptyPackages_ReturnsNoCatalogs()
    {
        // Prepare expected package
        var expectedPackages = new List<IWinGetPackage>();
        var catalog = ConfigureWinGetCatalogPackages(expectedPackages);

        // Act
        var loadedPackages = LoadCatalogsFromJsonDataSource("AppManagementPackages_Empty.json");

        // Assert
        Assert.AreEqual(0, loadedPackages.Count);
        catalog.Verify(c => c.GetPackagesAsync(It.IsAny<HashSet<string>>()), Times.Never());
    }

    [TestMethod]
    public void LoadCatalogs_ExceptionThrownWhenGettingPackages_ReturnsNoCatalogs()
    {
        // Configure winget catalog
        var catalogs = new Mock<IWinGetCatalog>();
        catalogs.Setup(c => c.GetPackagesAsync(It.IsAny<HashSet<string>>())).ThrowsAsync(new FindPackagesException(FindPackagesResultStatus.CatalogError));
        WindowsPackageManager!.Setup(wpm => wpm.WinGetCatalog).Returns(catalogs.Object);

        // Act
        var loadedPackages = LoadCatalogsFromJsonDataSource("AppManagementPackages_Success.json");

        // Assert
        Assert.AreEqual(0, loadedPackages.Count);
    }

    [TestMethod]
    public void LoadCatalogs_ExceptionThrownWhenOpeningFile_ThrowsException()
    {
        // Prepare expected package
        var expectedPackages = new List<IWinGetPackage>();
        ConfigureWinGetCatalogPackages(expectedPackages);

        // Act/Assert
        var fileName = TestHelpers.GetTestFilePath("file_not_found");
        var jsonDataSource = TestHost.CreateInstance<WinGetPackageJsonDataSource>(fileName);
        Assert.ThrowsException<FileNotFoundException>(() => jsonDataSource.InitializeAsync().GetAwaiter().GetResult());
        Assert.AreEqual(0, jsonDataSource.CatalogCount);
        Assert.AreEqual(0, jsonDataSource.LoadCatalogsAsync().GetAwaiter().GetResult().Count);
    }

    /// <summary>
    /// Configure winget catalog packages
    /// </summary>
    /// <param name="expectedPackages">Expected packages</param>
    /// <returns>Mock winget catalog</returns>
    private Mock<IWinGetCatalog> ConfigureWinGetCatalogPackages(IList<IWinGetPackage> expectedPackages)
    {
        var catalog = new Mock<IWinGetCatalog>();
        catalog.Setup(c => c.GetPackagesAsync(It.IsAny<HashSet<string>>())).ReturnsAsync(expectedPackages);
        WindowsPackageManager!.Setup(wpm => wpm.WinGetCatalog).Returns(catalog.Object);
        return catalog;
    }

    /// <summary>
    /// Load catalogs from a json data source
    /// </summary>
    /// <param name="fileName">Json file name</param>
    /// <returns>List of loaded package catalogs</returns>
    private IList<Models.PackageCatalog> LoadCatalogsFromJsonDataSource(string fileName)
    {
        var fileNamePath = TestHelpers.GetTestFilePath(fileName);
        var jsonDataSource = TestHost.CreateInstance<WinGetPackageJsonDataSource>(fileNamePath);
        jsonDataSource.InitializeAsync().GetAwaiter().GetResult();
        return jsonDataSource.LoadCatalogsAsync().GetAwaiter().GetResult();
    }
}
