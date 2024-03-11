// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ServiceProcess;
using HyperVExtension.Common.Extensions;
using HyperVExtension.Exceptions;
using HyperVExtension.Helpers;
using HyperVExtension.Models;
using HyperVExtension.Services;
using HyperVExtension.UnitTest.Mocks;
using Moq;
using TimeoutException = System.ServiceProcess.TimeoutException;

namespace HyperVExtension.UnitTest.HyperVExtensionTests.Services;

[TestClass]
public class HyperVManagerTest : HyperVExtensionTestsBase
{
    /// <summary>
    /// Gets common PowerShell results for loading the Hyper-V module and starting the VM management service.
    /// Use this when you need to mock functionality that uses the Hyper-V Manager
    /// </summary>
    [TestMethod]
    public void IsHyperVModuleLoadedReturnsFalseWhenModuleNotLoaded()
    {
        // Arrange
        SetupHyperVTestMethod(string.Empty, ServiceControllerStatus.Running);
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); });

        // Act
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        var actualValue = hyperVManager.IsHyperVModuleLoaded();

        // Assert
        Assert.IsFalse(actualValue);
    }

    [TestMethod]
    public void IsHyperVModuleLoadedReturnsTrueWhenModuleIsLoaded()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); });

        // Act
        var actualValue = hyperVManager.IsHyperVModuleLoaded();

        // Assert
        Assert.IsTrue(actualValue);
    }

    [TestMethod]
    [ExpectedException(typeof(HyperVAdminGroupException))]
    public void StartVirtualMachineManagementServiceFailsWhenUserNotInHyperVAdminGroup()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        var identityService = TestHost.GetService<IWindowsIdentityService>() as WindowsIdentityServiceMock;
        identityService!.SecuritySidIdentifier = string.Empty;
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); });

        // Assert
        hyperVManager.StartVirtualMachineManagementService();
    }

    [TestMethod]
    [ExpectedException(typeof(HyperVModuleNotLoadedException))]
    public void StartVirtualMachineManagementServiceFailsWhenModuleNotLoaded()
    {
        // Arrange
        SetupHyperVTestMethod(string.Empty, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); });

        // Assert
        hyperVManager.StartVirtualMachineManagementService();
    }

    [TestMethod]
    public void StartVirtualMachineManagementServiceDoesNotThrowWhenServiceIsRunning()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); });

        // If no exceptions are thrown, the service was started successfully
        hyperVManager.StartVirtualMachineManagementService();
    }

    [TestMethod]
    [ExpectedException(typeof(TimeoutException))]
    public void StartVirtualMachineManagementServiceThrowsExceptionWhenServiceNotRunning()
    {
        // Make sure the service appears to be stopped the next time we create an instance
        // of the IWindowsServiceController.
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Stopped);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); });

        // Assert
        hyperVManager.StartVirtualMachineManagementService();
    }

    [TestMethod]
    public void GetAllVirtualMachinesReturnsEmptyListWhenNoVMsExist()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); })
            .Returns(() => { return CreatePSObjectCollection(null); });

        // Act
        var virtualMachines = hyperVManager.GetAllVirtualMachines();
        var numberOfVirtualMachinesExpected = 0;

        // Assert
        Assert.IsNotNull(virtualMachines);
        Assert.AreEqual(numberOfVirtualMachinesExpected, virtualMachines.Count());
    }

    [TestMethod]
    public void GetAllVirtualMachinesReturnsListOfHyperVVirtualMachinesWhenTheyExist()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); })
            .Returns(() =>
            {
                // get two virtual machines.
                var collectionPsObjects = CreatePSObjectCollection(new PSCustomObjectMock());
                collectionPsObjects!.Add(new(new PSCustomObjectMock()));
                return collectionPsObjects;
            });

        // Act
        var virtualMachines = hyperVManager.GetAllVirtualMachines();
        var numberOfVirtualMachinesExpected = 2;

        // Assert
        Assert.IsNotNull(virtualMachines);
        Assert.AreEqual(numberOfVirtualMachinesExpected, virtualMachines.Count());
    }

    [TestMethod]
    public void GetVirtualMachineReturnsAHyperVVirtualMachineWhenItExists()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        var expectedVmGuid = Guid.NewGuid();
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); })
            .Returns(() =>
            {
                // get VM with Id = expectedVmGuid
                return CreatePSObjectCollection(new PSCustomObjectMock { Id = expectedVmGuid, });
            });

        // Act
        var virtualMachine = hyperVManager.GetVirtualMachine(expectedVmGuid);

        // Assert
        Assert.IsNotNull(virtualMachine);
        Assert.AreEqual(virtualMachine.Id, expectedVmGuid.ToString());
    }

    [TestMethod]
    public void StopVirtualMachineCanShutdownAVirtualMachine()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); })
            .Returns(() =>
            {
                // VM returned so we can check the state.
                return CreatePSObjectCollection(new PSCustomObjectMock { State = HyperVState.Off, });
            });

        // Act
        var wasVMShutdown = hyperVManager.StopVirtualMachine(Guid.NewGuid(), StopVMKind.Default);

        // Assert
        Assert.IsTrue(wasVMShutdown);
    }

    [TestMethod]
    public void StopVirtualMachineCanTurnOffAVirtualMachine()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); })
            .Returns(() =>
            {
                // Return VM that is in the off state.
                return CreatePSObjectCollection(new PSCustomObjectMock { State = HyperVState.Off, });
            });

        var wasVMTurnedOff = hyperVManager.StopVirtualMachine(Guid.NewGuid(), StopVMKind.TurnOff);

        // Assert
        Assert.IsTrue(wasVMTurnedOff);
    }

    [TestMethod]
    public void StopVirtualMachineCanSaveAVirtualMachinesState()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); })
            .Returns(() => { return CreatePSObjectCollection(new PSCustomObjectMock { State = HyperVState.Saved, }); });

        // Act
        var wasVMStateSaved = hyperVManager.StopVirtualMachine(Guid.NewGuid(), StopVMKind.Save);

        // Assert
        Assert.IsTrue(wasVMStateSaved);
    }

    [TestMethod]
    public void StartVirtualMachineCanStartAVirtualMachine()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); })
            .Returns(() => { return CreatePSObjectCollection(new PSCustomObjectMock { State = HyperVState.Running, }); });

        // Act
        var wasVMStarted = hyperVManager.StartVirtualMachine(Guid.NewGuid());

        // Assert
        Assert.IsTrue(wasVMStarted);
    }

    [TestMethod]
    public void PauseVirtualMachineCanPauseAVirtualMachine()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); })
            .Returns(() => { return CreatePSObjectCollection(new PSCustomObjectMock { State = HyperVState.Paused, }); });

        // Act
        var wasVMPaused = hyperVManager.PauseVirtualMachine(Guid.NewGuid());

        // Assert
        Assert.IsTrue(wasVMPaused);
    }

    [TestMethod]
    public void ResumeVirtualMachineCanResumeAVirtualMachine()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); })
            .Returns(() => { return CreatePSObjectCollection(new PSCustomObjectMock { State = HyperVState.Running, }); });

        // Act
        var wasVMResumed = hyperVManager.ResumeVirtualMachine(Guid.NewGuid());

        // Assert
        Assert.IsTrue(wasVMResumed);
    }

    [TestMethod]
    public void RemoveVirtualMachineCanRemoveAVirtualMachine()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); })
            .Returns(() => { return CreatePSObjectCollection(new PSCustomObjectMock { IsDeleted = true, }); });

        // Act
        var wasVMRemoved = hyperVManager.RemoveVirtualMachine(Guid.NewGuid());

        // Assert
        Assert.IsTrue(wasVMRemoved);
    }

    [TestMethod]
    public void ConnectToVirtualMachineDoesNotThrow()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); })
            .Returns(() =>
            {
                // Simulate PowerShell won't return an object here, so simulate this.
                return CreatePSObjectCollection(new PSCustomObjectMock());
            });

        // This should not throw an exception
        hyperVManager.ConnectToVirtualMachine(Guid.NewGuid());
    }

    [TestMethod]
    public void GetVirtualMachineCheckpointsReturnCheckpoints()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        var expectedCheckpointGuid = Guid.NewGuid();
        var checkpoint = CreatePSObjectCollection(new PSCustomObjectMock
        {
            Id = expectedCheckpointGuid,
            Name = "TestCheckpoint",
            ParentCheckpointId = Guid.NewGuid(),
            ParentCheckpointName = "TestCheckpointParent",
        });
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); })
            .Returns(() =>
            {
                // Simulate PowerShell returning a checkpoint.
                return checkpoint;
            });

        // Act
        var checkpoints = hyperVManager.GetVirtualMachineCheckpoints(Guid.NewGuid());
        var expectedCount = 1;

        // Assert
        Assert.AreEqual(expectedCount, checkpoints.Count());
        Assert.AreEqual(checkpoints.First().Id.ToString(), expectedCheckpointGuid.ToString());
    }

    [TestMethod]
    public void ApplyCheckpointToAVirtualMachine()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        var expectedCheckpointGuid = Guid.NewGuid();
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); })
            .Returns(() => { return CreatePSObjectCollection(new PSCustomObjectMock()); })
            .Returns(() =>
            {
                // Simulate PowerShell returning a VM whole parent checkpoint Id is now the Checkpoint Id of the one passed in.
                return CreatePSObjectCollection(new PSCustomObjectMock { ParentCheckpointId = expectedCheckpointGuid });
            });

        var wasCheckpointApplied = hyperVManager.ApplyCheckpoint(Guid.NewGuid(), expectedCheckpointGuid);

        // Assert
        Assert.IsTrue(wasCheckpointApplied);
    }

    [TestMethod]
    public void RemoveCheckpointFromAVirtualMachine()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        var initialCheckpointGuid = Guid.NewGuid();
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); })
            .Returns(() => { return CreatePSObjectCollection(new PSCustomObjectMock()); })
            .Returns(() =>
            {
                // Simulate PowerShell returning An empty object when no more checkpoints exist for the VM.
                return CreatePSObjectCollection(new PSCustomObjectMock());
            });

        var wasCheckpointRemoved = hyperVManager.RemoveCheckpoint(Guid.NewGuid(), initialCheckpointGuid);

        // Assert
        Assert.IsTrue(wasCheckpointRemoved);
    }

    [TestMethod]
    public void CreateCheckpointFromAVirtualMachine()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var hyperVManager = TestHost.GetService<IHyperVManager>();
        var newCheckpointId = Guid.NewGuid();
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(PowerShellHyperVModule); })
            .Returns(() =>
            {
                // Simulate PowerShell returning the new Checkpoint for the VM.
                return CreatePSObjectCollection(new PSCustomObjectMock { Id = newCheckpointId });
            });

        var wasCheckpointCreated = hyperVManager.CreateCheckpoint(Guid.NewGuid());

        // Assert
        Assert.IsTrue(wasCheckpointCreated);
    }
}
