// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.ServiceProcess;
using System.Text.Json;
using HyperVExtension.Common.Extensions;
using HyperVExtension.Helpers;
using HyperVExtension.Models.VirtualMachineCreation;
using HyperVExtension.UnitTest.Mocks;
using Microsoft.Windows.DevHome.SDK;
using Windows.Storage;

namespace HyperVExtension.UnitTest.HyperVExtensionTests.Services;

[TestClass]
public class HyperVProviderTests : HyperVExtensionTestsBase
{
    private readonly uint _logicalProcessorCount = 2;

    private readonly string _virtualHardDiskPath = "C:\\Users\\Public\\Downloads";

    private readonly string _virtualMachinePath = "C:\\Users\\Public\\Downloads";

    private readonly string _expectedVmName = "New Windows 11 VM";

    // 1024 * 10 to simulate 10GB
    public long MemoryMaximum => 10240;

    // Simulate 512MB
    public long MemoryMinimum => 512;

    private readonly string _tempFolderSaveLocation = Path.GetTempPath();

    public void OnProgressReceived(ICreateComputeSystemOperation operation, CreateComputeSystemProgressEventArgs progressArgs)
    {
    }

    [TestMethod]
    public async Task HyperVProvider_Can_Create_VirtualMachine()
    {
        // Arrange Hyper-V manager and powershell service
        SetupHyperVTestMethod(HyperVStrings.HyperVModuleName, ServiceControllerStatus.Running);

        // setup powershell session to return HyperVVirtualMachineHost object
        var objectForVirtualMachineHost = CreatePSObjectCollection(
            new PSCustomObjectMock()
            {
                VirtualHardDiskPath = _virtualHardDiskPath,
                VirtualMachinePath = _virtualMachinePath,
            });

        // setup powershell session to return HyperVVirtualMachine object
        var objectForVirtualMachine = CreatePSObjectCollection(
            new PSCustomObjectMock()
            {
                Name = _expectedVmName,
                MemoryMaximum = MemoryMaximum,
                MemoryMinimum = MemoryMinimum,
                LogicalProcessorCount = _logicalProcessorCount,
            });

        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return objectForVirtualMachineHost; })
            .Returns(() => { return objectForVirtualMachine; })
            .Returns(() => { return CreatePSObjectCollection(new PSCustomObjectMock()); }) // Calls Set-VMMemory to set VM startup memory but we don't need to check it
            .Returns(() => { return CreatePSObjectCollection(new PSCustomObjectMock()); }); // Calls Set-VMProcessor to set VM processor count but we don't need to check it

        // Arrange VMGalleryService HttpContent
        SetupGalleryHttpContent();

        var hyperVProvider = TestHost!.GetService<IComputeSystemProvider>();
        var inputJson = JsonSerializer.Serialize(new VMGalleryCreationUserInput()
        {
            NewEnvironmentName = _expectedVmName,
            SelectedImageListIndex = 0, // Our test gallery image list Json only has one image
        });

        var createComputeSystemOperation = hyperVProvider.CreateCreateComputeSystemOperation(null, inputJson);
        createComputeSystemOperation!.Progress += OnProgressReceived;

        // Act
        var createComputeSystemResult = await createComputeSystemOperation!.StartAsync();
        createComputeSystemOperation!.Progress -= OnProgressReceived;

        // Assert
        Assert.AreEqual(ProviderOperationStatus.Success, createComputeSystemResult.Result.Status);
        Assert.AreEqual(_expectedVmName, createComputeSystemResult.ComputeSystem.DisplayName);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        try
        {
            // Clean up temp folder
            var zipFileInTempFolder = await StorageFile.GetFileFromPathAsync($@"{_tempFolderSaveLocation}{GalleryDiskHash}.zip");
            await zipFileInTempFolder?.DeleteAsync();

            // cleanup public downloads folder
            var virtualHardDisk = await StorageFile.GetFileFromPathAsync($@"{_virtualHardDiskPath}\{_expectedVmName}.vhdx");
            await virtualHardDisk?.DeleteAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in Cleanup for Hyper-V provider test: {ex.Message}");
        }
    }
}
