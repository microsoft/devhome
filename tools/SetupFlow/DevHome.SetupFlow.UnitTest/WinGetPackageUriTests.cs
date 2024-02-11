// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.UnitTest;

[TestClass]
public class WinGetPackageUriTests
{
    [TestMethod]
    [DataRow("x-ms-winget://catalog/package")]
    [DataRow("x-ms-winget://catalog/package?version=1")]
    [DataRow("x-ms-winget://catalog/package?not_supported=1")]
    public void TryCreate_ValidUri_ReturnsTrue(string packageStringUri)
    {
        // Arrange
        var uri = new Uri(packageStringUri);

        // Act
        var result = WinGetPackageUri.TryCreate(uri, out var packageUri);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual("catalog", packageUri.CatalogName);
        Assert.AreEqual("package", packageUri.PackageId);
    }

    [TestMethod]
    [DataRow("x-ms-winget://catalog/package")]
    [DataRow("x-ms-winget://catalog/package?version=1")]
    [DataRow("x-ms-winget://catalog/package?not_supported=1")]
    public void TryCreate_ValidStringUri_ReturnsTrue(string packageStringUri)
    {
        // Act
        var result = WinGetPackageUri.TryCreate(packageStringUri, out var packageUri);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual("catalog", packageUri.CatalogName);
        Assert.AreEqual("package", packageUri.PackageId);
    }

    [TestMethod]
    public void TryCreate_NullStringUri_ReturnsFalse()
    {
        // Act
        var result = WinGetPackageUri.TryCreate(null as string, out var packageUri);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(packageUri);
    }

    [TestMethod]
    public void TryCreate_NullUri_ReturnsFalse()
    {
        // Act
        var result = WinGetPackageUri.TryCreate(null as Uri, out var packageUri);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(packageUri);
    }

    [TestMethod]
    public void TryCreate_InvalidUri_ReturnsFalse()
    {
        // Arrange
        var uri = new Uri("https://www.microsoft.com");

        // Act
        var result = WinGetPackageUri.TryCreate(uri, out var packageUri);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(packageUri);
    }

    [TestMethod]
    public void TryCreate_InvalidStringUri_ReturnsFalse()
    {
        // Act
        var result = WinGetPackageUri.TryCreate("https://www.microsoft.com", out var packageUri);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(packageUri);
    }

    [TestMethod]
    [DataRow("x-ms-winget://catalog/package?version=1", WinGetPackageUriParameters.All, "x-ms-winget://catalog/package?version=1")]
    [DataRow("x-ms-winget://catalog/package?version=1", WinGetPackageUriParameters.Version, "x-ms-winget://catalog/package?version=1")]
    [DataRow("x-ms-winget://catalog/package?version=1", WinGetPackageUriParameters.None, "x-ms-winget://catalog/package")]
    public void ToString_IncludeParameters_ReturnsUriString(
        string packageStringUri,
        WinGetPackageUriParameters includeParameters,
        string toString)
    {
        // Arrange
        WinGetPackageUri.TryCreate(packageStringUri, out var packageUri);

        // Act
        var result = packageUri.ToString(includeParameters);

        // Assert
        Assert.AreEqual(toString, result);
    }

    [TestMethod]
    [DataRow("x-ms-winget://catalog/package?version=1", "x-ms-winget://catalog/package?version=1", WinGetPackageUriParameters.All)]
    [DataRow("x-ms-winget://catalog/package?version=1", "x-ms-winget://catalog/package?version=1", WinGetPackageUriParameters.Version)]
    [DataRow("x-ms-winget://catalog/package?version=1", "x-ms-winget://catalog/package?version=1", WinGetPackageUriParameters.None)]
    [DataRow("x-ms-winget://catalog/package?version=1", "x-ms-winget://catalog/package?version=2", WinGetPackageUriParameters.None)]
    public void Equals_Uri_ReturnsTrue(string packageStringUri1, string packageStringUri2, WinGetPackageUriParameters includeParameters)
    {
        // Arrange
        WinGetPackageUri.TryCreate(packageStringUri1, out var packageUri1);
        WinGetPackageUri.TryCreate(packageStringUri2, out var packageUri2);

        // Act
        var result = packageUri1.Equals(packageUri2, includeParameters);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [DataRow("x-ms-winget://catalog1/package1?version=1", "x-ms-winget://catalog2/package1?version=1", WinGetPackageUriParameters.All)]
    [DataRow("x-ms-winget://catalog1/package1?version=1", "x-ms-winget://catalog1/package2?version=1", WinGetPackageUriParameters.All)]
    [DataRow("x-ms-winget://catalog1/package1?version=1", "x-ms-winget://catalog1/package1?version=2", WinGetPackageUriParameters.All)]
    [DataRow("x-ms-winget://catalog1/package1?version=1", "x-ms-winget://catalog1/package1?version=2", WinGetPackageUriParameters.Version)]
    [DataRow("x-ms-winget://catalog1/package1?version=1", "x-ms-winget://catalog2/package1?version=1", WinGetPackageUriParameters.None)]
    [DataRow("x-ms-winget://catalog1/package1?version=1", "x-ms-winget://catalog1/package2?version=1", WinGetPackageUriParameters.None)]
    public void Equals_Uri_ReturnsFalse(string packageStringUri1, string packageStringUri2, WinGetPackageUriParameters includeParameters)
    {
        // Arrange
        WinGetPackageUri.TryCreate(packageStringUri1, out var packageUri1);
        WinGetPackageUri.TryCreate(packageStringUri2, out var packageUri2);

        // Act
        var result = packageUri1.Equals(packageUri2, includeParameters);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Equals_NullUri_ReturnsFalse()
    {
        // Arrange
        WinGetPackageUri.TryCreate("x-ms-winget://catalog/package", out var packageUri);

        // Act
        var result = packageUri.Equals(null as WinGetPackageUri, WinGetPackageUriParameters.All);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Equals_UriAndStringUri_ReturnsTrue()
    {
        // Arrange
        WinGetPackageUri.TryCreate("x-ms-winget://catalog/package?version=1", out var packageUri);

        // Act
        var result = packageUri.Equals("x-ms-winget://catalog/package?version=1", WinGetPackageUriParameters.All);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [DataRow("x-ms-winget://catalog/package")]
    [DataRow("x-ms-winget://catalog/package?version=1")]
    public void Constructor_ValidUri_InitializesProperties(string uri)
    {
        // Act
        var packageUri = new WinGetPackageUri(uri);

        // Assert
        Assert.AreEqual("catalog", packageUri.CatalogName);
        Assert.AreEqual("package", packageUri.PackageId);
        Assert.IsNotNull(packageUri.Options);
    }

    [TestMethod]
    [DataRow("https://catalog/package")]
    [DataRow("x-ms-winget://catalog?version=1")]
    [DataRow("x-ms-winget://")]
    [DataRow("x-ms-winget://?version=1")]
    public void Constructor_InvalidUri_ThrowsException(string uri)
    {
        // Act/Assert
        Assert.ThrowsException<UriFormatException>(() => new WinGetPackageUri(uri));
    }
}
