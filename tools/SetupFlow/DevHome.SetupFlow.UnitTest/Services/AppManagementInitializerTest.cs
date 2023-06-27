// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

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
            .Setup(wpm => wpm.IsCOMServerAvailableAsync())
            .ReturnsAsync(true);

        // Act
        var initializer = TestHost.GetService<IAppManagementInitializer>();
        initializer.InitializeAsync().GetAwaiter().GetResult();

        // Assert
        WindowsPackageManager
            .Verify(dep => dep.RegisterAppInstallerAsync(), Times.Never);
        WindowsPackageManager
            .Verify(wpm => wpm.ConnectToAllCatalogsAsync(It.IsAny<bool>()), Times.Once);
        WindowsPackageManager
            .Verify(wpm => wpm.IsCOMServerAvailableAsync(), Times.Once);
    }

    [TestMethod]
    public void Initialize_COMServerNotAvailable_ShouldRegisterAndConnectToCatalogs()
    {
        // Setup
        WindowsPackageManager
            .SetupSequence(wpm => wpm.IsCOMServerAvailableAsync())
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
            .Verify(wpm => wpm.ConnectToAllCatalogsAsync(It.IsAny<bool>()), Times.Once);
        WindowsPackageManager
            .Verify(wpm => wpm.IsCOMServerAvailableAsync(), Times.Exactly(2));
    }

    [TestMethod]
    public void Initialize_RegistrationFailed_ShouldNotConnectToCatalogs()
    {
        // Setup
        WindowsPackageManager
            .Setup(wpm => wpm.IsCOMServerAvailableAsync())
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
            .Verify(wpm => wpm.ConnectToAllCatalogsAsync(It.IsAny<bool>()), Times.Never);
        WindowsPackageManager
            .Verify(wpm => wpm.IsCOMServerAvailableAsync(), Times.Once);
    }

    [TestMethod]
    public void Initialize_COMServerNotAvailableAfterSuccessfulRegistration_ShouldNotConnectToCatalogs()
    {
        // Setup
        WindowsPackageManager
            .Setup(wpm => wpm.IsCOMServerAvailableAsync())
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
            .Verify(wpm => wpm.ConnectToAllCatalogsAsync(It.IsAny<bool>()), Times.Never);
        WindowsPackageManager
            .Verify(wpm => wpm.IsCOMServerAvailableAsync(), Times.Exactly(2));
    }
}
