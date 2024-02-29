// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Services;
using DevHome.Contracts.Services;
using DevHome.Services;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
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

    protected Mock<ISetupFlowStringResource> StringResource { get; private set; }

    protected IHost TestHost { get; private set; }
#pragma warning restore CS8618 // Non-nullable properties initialized in [TestInitialize]

    [TestInitialize]
    public void TestInitialize()
    {
        WindowsPackageManager = new Mock<IWindowsPackageManager>();
        ThemeSelectorService = new Mock<IThemeSelectorService>();
        RestoreInfo = new Mock<IRestoreInfo>();
        StringResource = new Mock<ISetupFlowStringResource>();
        TestHost = CreateTestHost();

        // Configure string resource localization to return the input key by default
        StringResource
            .Setup(sr => sr.GetLocalized(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] args) => key);
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
                services.AddSingleton<IThemeSelectorService>(ThemeSelectorService!.Object);
                services.AddSingleton<ISetupFlowStringResource>(StringResource.Object);
                services.AddSingleton<SetupFlowOrchestrator>(new SetupFlowOrchestrator());
                services.AddSingleton<IExtensionService>(new ExtensionService());

                // App-management view models
                services.AddTransient<PackageViewModel>();
                services.AddTransient<PackageCatalogViewModel>();
                services.AddTransient<SearchViewModel>();
                services.AddTransient<LoadingViewModel>();
                services.AddTransient<IDevDriveManager, DevDriveManager>();

                // App-management services
                services.AddSingleton<IWindowsPackageManager>(WindowsPackageManager.Object);
                services.AddTransient<WinGetPackageJsonDataSource>();
                services.AddTransient<WinGetPackageRestoreDataSource>();
                services.AddSingleton<IRestoreInfo>(RestoreInfo.Object);
                services.AddSingleton<PackageProvider>();
                services.AddSingleton<WindowsPackageManagerFactory>(new WindowsPackageManagerDefaultFactory());
                services.AddSingleton<IAppManagementInitializer, AppManagementInitializer>();
                services.AddSingleton<WinGetPackageDataSource, WinGetPackageRestoreDataSource>();
                services.AddSingleton<ICatalogDataSourceLoader, CatalogDataSourceLoader>();
                services.AddSingleton<IScreenReaderService>(new Mock<IScreenReaderService>().Object);
                services.AddSingleton<IDesiredStateConfiguration>(new Mock<IDesiredStateConfiguration>().Object);

                // DI factory pattern
                services.AddSingleton<PackageViewModelFactory>(sp => package => ActivatorUtilities.CreateInstance<PackageViewModel>(sp, package));
                services.AddSingleton<PackageCatalogViewModelFactory>(sp => catalog => ActivatorUtilities.CreateInstance<PackageCatalogViewModel>(sp, catalog));
            }).Build();
    }
}
