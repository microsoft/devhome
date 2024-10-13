// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Models.ExtensionJsonData;
using DevHome.Common.Services;
using DevHome.Services;
using DevHome.Services.Core.Contracts;
using DevHome.Services.WindowsPackageManager.Contracts;
using DevHome.Services.WindowsPackageManager.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace DevHome.Test.FunctionalTests;

[TestClass]
public class ExtensionServiceTests
{
    private readonly Mock<ILocalSettingsService> _localSettingsService = new();

    private readonly Mock<IStringResource> _stringResouce = new();

    private readonly Mock<IPackageDeploymentService> _deploymentService = new();

    private readonly Mock<IWinGet> _winGet = new();

    private readonly WinGetPackageUri _packageUri = new("x-ms-winget://msstore/9MV8F79FGXTR");

    private readonly Mock<IWinGetPackage> _winGetPackage = new();

    private async Task<IList<IWinGetPackage>> MockGetPackagesAsync()
    {
        await Task.CompletedTask;
        return new List<IWinGetPackage>() { _winGetPackage.Object };
    }

    private IHost? _hostService;

    [TestInitialize]
    public void TestInitialize()
    {
        // Setup WinGet to return at least one valid package.
        _winGet
            .Setup(manager => manager.CreateMsStoreCatalogPackageUri(It.IsAny<string>()))
            .Returns(_packageUri);

        _winGet
            .Setup(manager => manager.GetPackagesAsync(It.IsAny<List<WinGetPackageUri>>()))
            .Returns(MockGetPackagesAsync);

        _winGetPackage.Setup(package => package.Id).Returns("9MV8F79FGXTR");

        _hostService = CreateHost();

        _stringResouce
            .Setup(strResource => strResource.GetLocalized(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] args) => key);

        _localSettingsService.Setup(localSettingsService =>
            localSettingsService.GetPathToPackageLocation())
            .Returns(Directory.GetCurrentDirectory());
    }

    /// <summary>
    /// In this test we're specifically testing that we can retrieve the extension information
    /// from GitHub and deserialize it to a <see cref="DevHomeExtensionContentData"/>
    /// </summary>
    [TestMethod]
    public async Task ExtensionServiceReturnsValidExtensionJsonDataObject()
    {
        var extensionService = _hostService!.GetService<IExtensionService>();
        var extensionJsonData = await extensionService.GetExtensionJsonDataAsync();
        Assert.IsNotNull(extensionJsonData);

        Assert.IsInstanceOfType(extensionJsonData, typeof(DevHomeExtensionContentData));
    }

    private IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(_localSettingsService!.Object);
                services.AddSingleton(_stringResouce!.Object);
                services.AddSingleton(_winGet!.Object);
                services.AddSingleton(_deploymentService!.Object);
                services.AddSingleton<IExtensionService, ExtensionService>();
                services.AddHttpClient();
            }).Build();
    }
}
