// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using System.ServiceProcess;
using HyperVExtension.Common.Extensions;
using HyperVExtension.Helpers;
using HyperVExtension.Models;
using HyperVExtension.Services;
using HyperVExtension.UnitTest.Mocks;
using Microsoft.Windows.DevHome.SDK;

namespace HyperVExtension.UnitTest.HyperVExtensionTests.Services;

[TestClass]
public class HyperVVirtualMachineTest : HyperVExtensionTestsBase
{
    private readonly PSCustomObjectMock _psVirtualMachineObject = new()
    {
        Id = Guid.Parse("be3776d4-5082-4ca6-b352-58543365ba2d"),
        ParentCheckpointId = Guid.Parse("bcd583ed-f857-4182-9f77-d13ccb6032f2"),
    };

    private readonly Checkpoint _psCheckpointAfterItWasCreated = new()
    {
        ParentCheckpointId = Guid.Parse("bcd583ed-f857-4182-9f77-d13ccb6032f2"),
        ParentCheckpointName = "ParentCheckPoint",
        Id = Guid.Parse("be3776d4-5082-4ca6-b352-58543365ba2d"),
        Name = "CurrentCheckPoint",
    };

    private readonly Checkpoint _psVirtualMachineObjectAfterDeletingCheckpoint = new()
    {
        Id = Guid.Parse("be3776d4-5082-4ca6-b352-58543365ba2d"),
        ParentCheckpointId = Guid.Parse("00000000-0000-0000-0000-000000000000"),
    };

    private readonly PSCustomObjectMock _psVirtualMachineObjectAfterDeletion = new()
    {
        IsDeleted = true,
    };

    [TestMethod]
    public async Task HyperVVirtualMachineCanStartSuccessfully()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        _psVirtualMachineObject.State = HyperVState.Running;
        var expectedPsObjectCollection = CreatePSObjectCollection(_psVirtualMachineObject);
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return expectedPsObjectCollection; });
        var expectedProviderOperationStatus = ProviderOperationStatus.Success;
        var hyperVManager = TestHost!.GetService<IHyperVManager>();
        var hyperVVirtualMachineFactory = TestHost!.GetService<HyperVVirtualMachineFactory>();
        var hyperVVirtualMachine = hyperVVirtualMachineFactory(expectedPsObjectCollection.First());

        // Act
        var computeSystemOperationResult = await hyperVVirtualMachine.StartAsync(string.Empty);

        // Assert
        Assert.IsNotNull(computeSystemOperationResult);
        Assert.AreEqual(expectedProviderOperationStatus, computeSystemOperationResult.Result.Status);
    }

    [TestMethod]
    public async Task HyperVVirtualMachineCanShutdownSuccessfully()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        _psVirtualMachineObject.State = HyperVState.Off;
        var expectedPsObjectCollection = CreatePSObjectCollection(_psVirtualMachineObject);
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return expectedPsObjectCollection; });
        var expectedProviderOperationStatus = ProviderOperationStatus.Success;
        var hyperVManager = TestHost!.GetService<IHyperVManager>();
        var hyperVVirtualMachineFactory = TestHost!.GetService<HyperVVirtualMachineFactory>();
        var hyperVVirtualMachine = hyperVVirtualMachineFactory(expectedPsObjectCollection.First());

        // Act
        var computeSystemOperationResult = await hyperVVirtualMachine.ShutDownAsync(string.Empty);

        // Assert
        Assert.IsNotNull(computeSystemOperationResult);
        Assert.AreEqual(expectedProviderOperationStatus, computeSystemOperationResult.Result.Status);
    }

    [TestMethod]
    public async Task HyperVVirtualMachineCanTerminateSuccessfully()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        _psVirtualMachineObject.State = HyperVState.Off;
        var expectedPsObjectCollection = CreatePSObjectCollection(_psVirtualMachineObject);
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return expectedPsObjectCollection; });
        var expectedProviderOperationStatus = ProviderOperationStatus.Success;
        var hyperVManager = TestHost!.GetService<IHyperVManager>();
        var hyperVVirtualMachineFactory = TestHost!.GetService<HyperVVirtualMachineFactory>();
        var hyperVVirtualMachine = hyperVVirtualMachineFactory(expectedPsObjectCollection.First());

        // Act
        var computeSystemOperationResult = await hyperVVirtualMachine.TerminateAsync(string.Empty);

        // Assert
        Assert.IsNotNull(computeSystemOperationResult);
        Assert.AreEqual(expectedProviderOperationStatus, computeSystemOperationResult.Result.Status);
    }

    [TestMethod]
    public async Task HyperVVirtualMachineCanBeDeletedSuccessfully()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        var expectedPsObjectCollection = CreatePSObjectCollection(_psVirtualMachineObjectAfterDeletion);
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return expectedPsObjectCollection; });
        var expectedProviderOperationStatus = ProviderOperationStatus.Success;
        var hyperVManager = TestHost!.GetService<IHyperVManager>();
        var hyperVVirtualMachineFactory = TestHost!.GetService<HyperVVirtualMachineFactory>();
        var hyperVVirtualMachine = hyperVVirtualMachineFactory(expectedPsObjectCollection.First());

        // Act
        var computeSystemOperationResult = await hyperVVirtualMachine.DeleteAsync(string.Empty);

        // Assert
        Assert.IsNotNull(computeSystemOperationResult);
        Assert.AreEqual(expectedProviderOperationStatus, computeSystemOperationResult.Result.Status);
    }

    [TestMethod]
    public async Task HyperVVirtualMachineCanSaveItsStateSuccessfully()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        _psVirtualMachineObject.State = HyperVState.Saved;
        var expectedPsObjectCollection = CreatePSObjectCollection(_psVirtualMachineObject);
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return expectedPsObjectCollection; });
        var expectedProviderOperationStatus = ProviderOperationStatus.Success;
        var hyperVManager = TestHost!.GetService<IHyperVManager>();
        var hyperVVirtualMachineFactory = TestHost!.GetService<HyperVVirtualMachineFactory>();
        var hyperVVirtualMachine = hyperVVirtualMachineFactory(expectedPsObjectCollection.First());

        // Act
        var computeSystemOperationResult = await hyperVVirtualMachine.SaveAsync(string.Empty);

        // Assert
        Assert.IsNotNull(computeSystemOperationResult);
        Assert.AreEqual(expectedProviderOperationStatus, computeSystemOperationResult.Result.Status);
    }

    [TestMethod]
    public async Task HyperVVirtualMachineCanPauseItsStateSuccessfully()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        _psVirtualMachineObject.State = HyperVState.Paused;
        var expectedPsObjectCollection = CreatePSObjectCollection(_psVirtualMachineObject);
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return expectedPsObjectCollection; });
        var expectedProviderOperationStatus = ProviderOperationStatus.Success;
        var hyperVManager = TestHost!.GetService<IHyperVManager>();
        var hyperVVirtualMachineFactory = TestHost!.GetService<HyperVVirtualMachineFactory>();
        var hyperVVirtualMachine = hyperVVirtualMachineFactory(expectedPsObjectCollection.First());

        // Act
        var computeSystemOperationResult = await hyperVVirtualMachine.PauseAsync(string.Empty);

        // Assert
        Assert.IsNotNull(computeSystemOperationResult);
        Assert.AreEqual(expectedProviderOperationStatus, computeSystemOperationResult.Result.Status);
    }

    [TestMethod]
    public async Task HyperVVirtualMachineCanResumeItsStateSuccessfully()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        _psVirtualMachineObject.State = HyperVState.Running;
        var expectedPsObjectCollection = CreatePSObjectCollection(_psVirtualMachineObject);
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return expectedPsObjectCollection; });
        var expectedProviderOperationStatus = ProviderOperationStatus.Success;
        var hyperVManager = TestHost!.GetService<IHyperVManager>();
        var hyperVVirtualMachineFactory = TestHost!.GetService<HyperVVirtualMachineFactory>();
        var hyperVVirtualMachine = hyperVVirtualMachineFactory(expectedPsObjectCollection.First());

        // Act
        var computeSystemOperationResult = await hyperVVirtualMachine.ResumeAsync(string.Empty);

        // Assert
        Assert.IsNotNull(computeSystemOperationResult);
        Assert.AreEqual(expectedProviderOperationStatus, computeSystemOperationResult.Result.Status);
    }

    [TestMethod]
    public async Task HyperVVirtualMachineCanCreateCheckpointSuccessfully()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        _psVirtualMachineObject.State = HyperVState.Running;
        var expectedPsObjectCollection = CreatePSObjectCollection(_psCheckpointAfterItWasCreated);
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return expectedPsObjectCollection; });
        var expectedProviderOperationStatus = ProviderOperationStatus.Success;
        var hyperVManager = TestHost!.GetService<IHyperVManager>();
        var hyperVVirtualMachineFactory = TestHost!.GetService<HyperVVirtualMachineFactory>();
        var hyperVVirtualMachine = hyperVVirtualMachineFactory(new PSObject(_psVirtualMachineObject));

        // Act
        var computeSystemOperationResult = await hyperVVirtualMachine.CreateSnapshotAsync(string.Empty);

        // Assert
        Assert.IsNotNull(computeSystemOperationResult);
        Assert.AreEqual(expectedProviderOperationStatus, computeSystemOperationResult.Result.Status);
    }

    [TestMethod]
    public async Task HyperVVirtualMachineCanRevertCheckpointSuccessfully()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        _psVirtualMachineObject.State = HyperVState.Running;
        var expectedPsObjectCollection = CreatePSObjectCollection(_psVirtualMachineObject);
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return expectedPsObjectCollection; })
            .Returns(() => { return expectedPsObjectCollection; });
        var expectedProviderOperationStatus = ProviderOperationStatus.Success;
        var hyperVManager = TestHost!.GetService<IHyperVManager>();
        var hyperVVirtualMachineFactory = TestHost!.GetService<HyperVVirtualMachineFactory>();
        var hyperVVirtualMachine = hyperVVirtualMachineFactory(new PSObject(_psVirtualMachineObject));

        // Act
        var computeSystemOperationResult = await hyperVVirtualMachine.RevertSnapshotAsync(string.Empty);

        // Assert
        Assert.IsNotNull(computeSystemOperationResult);
        Assert.AreEqual(expectedProviderOperationStatus, computeSystemOperationResult.Result.Status);
    }

    [TestMethod]
    public async Task HyperVVirtualMachineCanDeleteCheckpointSuccessfully()
    {
        // Arrange
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);
        _psVirtualMachineObject.State = HyperVState.Running;
        var initialReturnedPsObjectCollection = CreatePSObjectCollection(_psVirtualMachineObject);
        var psObjectCollectionReturnedAfterDeletion = CreatePSObjectCollection(_psVirtualMachineObjectAfterDeletingCheckpoint);
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return initialReturnedPsObjectCollection; })
            .Returns(() => { return psObjectCollectionReturnedAfterDeletion; });
        var expectedProviderOperationStatus = ProviderOperationStatus.Success;
        var hyperVManager = TestHost!.GetService<IHyperVManager>();
        var hyperVVirtualMachineFactory = TestHost!.GetService<HyperVVirtualMachineFactory>();
        var hyperVVirtualMachine = hyperVVirtualMachineFactory(initialReturnedPsObjectCollection.First());

        // Act
        var computeSystemOperationResult = await hyperVVirtualMachine.DeleteSnapshotAsync(string.Empty);

        // Assert
        Assert.IsNotNull(computeSystemOperationResult);
        Assert.AreEqual(expectedProviderOperationStatus, computeSystemOperationResult.Result.Status);
    }
}
