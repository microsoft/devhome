// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.SetupFlow.Services;
using Moq;

namespace DevHome.SetupFlow.UnitTest.Services;

[TestClass]
public class AppManagementInitializerTest : BaseSetupFlowTest
{
    [TestMethod]
    public void Initialize_COMServerAvailable_ShouldNotRegisterAndShouldConnectToCatalogs()
    {
        // Setup
        WindowsPackageManager
            .Setup(wpm => wpm.IsAvailableAsync())
            .ReturnsAsync(true);

        // Act
        var initializer = TestHost.GetService<IAppManagementInitializer>();
        initializer.InitializeAsync().GetAwaiter().GetResult();

        // Assert
        WindowsPackageManager
            .Verify(dep => dep.RegisterAppInstallerAsync(), Times.Never);
        WindowsPackageManager
            .Verify(wpm => wpm.InitializeAsync(), Times.Once);
        WindowsPackageManager
            .Verify(wpm => wpm.IsAvailableAsync(), Times.Once);
    }

    [TestMethod]
    public void Initialize_COMServerNotAvailable_ShouldRegisterAndConnectToCatalogs()
    {
        // Setup
        WindowsPackageManager
            .SetupSequence(wpm => wpm.IsAvailableAsync())
            .ReturnsAsync(false)
            .ReturnsAsync(true);
        WindowsPackageManager.
            Setup(wpm => wpm.RegisterAppInstallerAsync())
            .ReturnsAsync(true);

        // Act
        var initializer = TestHost.GetService<IAppManagementInitializer>();
        initializer.InitializeAsync().GetAwaiter().GetResult();

        // Assert
        WindowsPackageManager
            .Verify(dep => dep.RegisterAppInstallerAsync(), Times.Once);
        WindowsPackageManager
            .Verify(wpm => wpm.InitializeAsync(), Times.Once);
        WindowsPackageManager
            .Verify(wpm => wpm.IsAvailableAsync(), Times.Exactly(2));
    }

    [TestMethod]
    public void Initialize_RegistrationFailed_ShouldNotConnectToCatalogs()
    {
        // Setup
        WindowsPackageManager
            .Setup(wpm => wpm.IsAvailableAsync())
            .ReturnsAsync(false);
        WindowsPackageManager.
            Setup(wpm => wpm.RegisterAppInstallerAsync())
            .ReturnsAsync(false);

        // Act
        var initializer = TestHost.GetService<IAppManagementInitializer>();
        initializer.InitializeAsync().GetAwaiter().GetResult();

        // Assert
        WindowsPackageManager
            .Verify(dep => dep.RegisterAppInstallerAsync(), Times.Once);
        WindowsPackageManager
            .Verify(wpm => wpm.InitializeAsync(), Times.Never);
        WindowsPackageManager
            .Verify(wpm => wpm.IsAvailableAsync(), Times.Once);
    }

    [TestMethod]
    public void Initialize_COMServerNotAvailableAfterSuccessfulRegistration_ShouldNotConnectToCatalogs()
    {
        // Setup
        WindowsPackageManager
            .Setup(wpm => wpm.IsAvailableAsync())
            .ReturnsAsync(false);
        WindowsPackageManager.
            Setup(wpm => wpm.RegisterAppInstallerAsync())
            .ReturnsAsync(true);

        // Act
        var initializer = TestHost.GetService<IAppManagementInitializer>();
        initializer.InitializeAsync().GetAwaiter().GetResult();

        // Assert
        WindowsPackageManager
            .Verify(dep => dep.RegisterAppInstallerAsync(), Times.Once);
        WindowsPackageManager
            .Verify(wpm => wpm.InitializeAsync(), Times.Never);
        WindowsPackageManager
            .Verify(wpm => wpm.IsAvailableAsync(), Times.Exactly(2));
    }
}
