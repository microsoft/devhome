// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Services.WindowsPackageManager.Contracts;
using DevHome.SetupFlow.Models;
using Moq;

namespace DevHome.SetupFlow.UnitTest.Helpers;

public class PackageHelper
{
    public static Mock<IWinGetPackage> CreatePackage(string id, string catalogId = "mock catalog id")
    {
        var package = new Mock<IWinGetPackage>();
        package.Setup(p => p.Id).Returns(id);
        package.Setup(p => p.CatalogId).Returns(catalogId);
        package.Setup(p => p.UniqueKey).Returns(new PackageUniqueKey(id, catalogId));
        package.Setup(p => p.Name).Returns("Mock Package Name");
        package.Setup(p => p.PackageUrl).Returns(new Uri("https://packageUrl"));
        package.Setup(p => p.PublisherUrl).Returns(new Uri("https://publisherUrl"));
        package.Setup(p => p.InstalledVersion).Returns("Mock Version");

        // Allow icon properties to be set and get like regular properties
        package.SetupProperty(p => p.LightThemeIcon);
        package.SetupProperty(p => p.DarkThemeIcon);

        return package;
    }

    public static PackageCatalog CreatePackageCatalog(int packageCount, Action<PackageCatalog>? customizeCatalog = null)
    {
        var packageCatalog = new PackageCatalog()
        {
            Name = "Mock PackageCatalog Name",
            Description = "Mock PackageCatalog Description",
            Packages = Enumerable.Range(1, packageCount).Select(x => CreatePackage($"{x}").Object).ToList(),
        };
        customizeCatalog?.Invoke(packageCatalog);
        return packageCatalog;
    }
}
