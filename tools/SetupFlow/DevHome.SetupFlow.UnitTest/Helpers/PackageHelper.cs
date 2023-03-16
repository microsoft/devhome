// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.AppManagement.Models;
using Moq;

namespace DevHome.SetupFlow.UnitTest.Helpers;
public class PackageHelper
{
    public static Mock<IWinGetPackage> CreatePackage(string id)
    {
        var package = new Mock<IWinGetPackage>();
        package.Setup(p => p.Id).Returns(id);
        package.Setup(p => p.Name).Returns("Mock Package Name");
        package.Setup(p => p.ImageUri).Returns(new Uri("https://mock/"));
        package.Setup(p => p.PackageUri).Returns(new Uri("https://microsoft.com"));
        package.Setup(p => p.Version).Returns("Mock Version");
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
