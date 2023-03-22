// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Contracts.Services;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.SetupFlow.AppManagement.ViewModels;
using DevHome.SetupFlow.Common.Services;
using DevHome.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Internal.Windows.DevHome.Helpers.Restore;
using Moq;

namespace DevHome.SetupFlow.UnitTest;

/// <summary>
/// Base class for setup flow unit tests
/// </summary>
public class BaseSetupFlowTest
{
#pragma warning disable CS8618 // Non-nullable properties initialized in [TestInitialize]
    protected Mock<IWindowsPackageManager> WindowsPackageManager { get; private set; }

    protected Mock<IThemeSelectorService> ThemeSelectorService { get; private set; }

    protected Mock<IRestoreInfo> RestoreInfo { get; private set; }

    protected IHost TestHost { get; private set; }
#pragma warning restore CS8618 // Non-nullable properties initialized in [TestInitialize]

    [TestInitialize]
    public void TestInitialize()
    {
        WindowsPackageManager = new Mock<IWindowsPackageManager>();
        ThemeSelectorService = new Mock<IThemeSelectorService>();
        RestoreInfo = new Mock<IRestoreInfo>();
        TestHost = CreateTestHost();
    }

    /// <summary>
    /// Create a test host with mock service instances
    /// </summary>
    /// <returns>Test host</returns>
    private IHost CreateTestHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // Common services
                services.AddSingleton<ILogger>(new Mock<ILogger>().Object);
                services.AddSingleton<IThemeSelectorService>(ThemeSelectorService!.Object);
                services.AddSingleton<ISetupFlowStringResource>(_ =>
                {
                    // Configure string resource localization to return the input key
                    var stringResource = new Mock<ISetupFlowStringResource>();
                    stringResource
                        .Setup(sr => sr.GetLocalized(It.IsAny<string>(), It.IsAny<object[]>()))
                        .Returns((string key, object[] args) => key);
                    return stringResource.Object;
                });

                // App-management view models
                services.AddTransient<PackageViewModel>();
                services.AddTransient<PackageCatalogViewModel>();
                services.AddTransient<SearchViewModel>();

                // App-management services
                services.AddSingleton<IWindowsPackageManager>(WindowsPackageManager.Object);
                services.AddTransient<WinGetPackageJsonDataSource>();
                services.AddTransient<WinGetPackageRestoreDataSource>();
                services.AddSingleton<IRestoreInfo>(RestoreInfo.Object);
            }).Build();
    }
}
