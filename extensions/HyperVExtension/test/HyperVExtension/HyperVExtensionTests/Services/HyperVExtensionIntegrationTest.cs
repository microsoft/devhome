// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using HyperVExtension.Common;
using HyperVExtension.Common.Extensions;
using HyperVExtension.Helpers;
using HyperVExtension.Models;
using HyperVExtension.Models.VirtualMachineCreation;
using HyperVExtension.Models.VMGalleryJsonToClasses;
using HyperVExtension.Providers;
using HyperVExtension.Services;
using HyperVExtension.UnitTest.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.SDK;
using Moq;
using Windows.Storage;

namespace HyperVExtension.UnitTest.HyperVExtensionTests.Services;

/// <summary>
/// Hyper-V extension integration tests.
/// </summary>
[TestClass]
public class HyperVExtensionIntegrationTest
{
    private readonly string tempFileNameToWriteProgress = "HyperVVMCreationProgress";

    private readonly object _testFileWriteLock = new();

    public uint PreviousPercentage { get; private set; }

    protected Mock<IStringResource>? MockedStringResource
    {
        get; set;
    }

    protected IHost? TestHost
    {
        get; set;
    }

    private sealed class OperationData
    {
        public OperationData()
        {
        }

        public List<ConfigurationSetChangeData> ProgressData { get; } = new();

        public ApplyConfigurationResult? ConfigurationResult { get; set; }

        public ManualResetEvent Completed { get; } = new ManualResetEvent(false);
    }

    [TestInitialize]
    public void TestInitialize()
    {
        MockedStringResource = new Mock<IStringResource>();
        TestHost = CreateTestHost();

        // Configure string resource localization to return the input key by default
        MockedStringResource
            .Setup(strResource => strResource.GetLocalized(It.IsAny<string>(), It.IsAny<object[]>()))
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
                // Services
                services.AddHttpClient();
                services.AddSingleton<IStringResource>(MockedStringResource!.Object);
                services.AddSingleton<IComputeSystemProvider, HyperVProvider>();
                services.AddSingleton<HyperVExtension>();
                services.AddSingleton<IHyperVManager, HyperVManager>();
                services.AddSingleton<IWindowsIdentityService, WindowsIdentityServiceMock>();
                services.AddSingleton<IVMGalleryService, VMGalleryService>();
                services.AddSingleton<IArchiveProviderFactory, ArchiveProviderFactory>();
                services.AddSingleton<IDownloaderService, DownloaderService>();

                // Pattern to allow multiple non-service registered interfaces to be used with registered interfaces during construction.
                services.AddSingleton<IPowerShellService>(psService =>
                    ActivatorUtilities.CreateInstance<PowerShellService>(psService, new PowerShellSession()));
                services.AddSingleton<HyperVVirtualMachineFactory>(serviceProvider => psObject => ActivatorUtilities.CreateInstance<HyperVVirtualMachine>(serviceProvider, psObject));
                services.AddSingleton<VmGalleryCreationOperationFactory>(serviceProvider => parameters => ActivatorUtilities.CreateInstance<VMGalleryVMCreationOperation>(serviceProvider, parameters));

                services.AddTransient<IWindowsServiceController, WindowsServiceController>();
            }).Build();
    }

    /// <summary>
    /// Requirements to run this test:
    ///   Hyper-V VM named "TestVM" running
    ///   DevSetupAgent service is installed and running
    ///   Developer mode enabled (DeveloperMode task in Yaml required elevation and will fail,
    ///     but if it's already enabled it will succeed.)
    ///   User logged on to the VM.
    /// </summary>
    [TestMethod]
    public async Task TestConfigureRequest()
    {
        IHyperVManager hyperVManager = TestHost!.GetService<IHyperVManager>();
        var machines = hyperVManager.GetAllVirtualMachines();
        HyperVVirtualMachine? testVm = null;
        foreach (var vm in machines)
        {
            if (string.Equals(vm.DisplayName, "TestVM", StringComparison.OrdinalIgnoreCase))
            {
                testVm = vm;
                break;
            }
        }

        Assert.IsNotNull(testVm);

        var configurationYaml =
@"# yaml-language-server: $schema=https://aka.ms/configuration-dsc-schema/0.2
properties:
  assertions:
    - resource: Microsoft.Windows.Developer/OsVersion
      directives:
        description: Verify min OS version requirement
        allowPrerelease: true
      settings:
        MinVersion: '10.0.22000'
  resources:
    - resource: Microsoft.Windows.Developer/DeveloperMode
      directives:
        description: Enable Developer Mode
        allowPrerelease: true
      settings:
        Ensure: Present
  configurationVersion: 0.2.0";

        var operationData = new OperationData();
        var operation = testVm.CreateApplyConfigurationOperation(configurationYaml)!;

        operation.ConfigurationSetStateChanged += (sender, progressData) =>
        {
            operationData.ProgressData.Add(progressData.ConfigurationSetChangeData);
            PrintProgressData(progressData.ConfigurationSetChangeData);
        };

        operation.ActionRequired += async (sender, actionRequired) =>
        {
            if (actionRequired.CorrectiveActionCardSession is VmCredentialAdaptiveCardSession credentialsCardSession)
            {
               var extensionAdaptiveCard = new Mock<IExtensionAdaptiveCard>();
               extensionAdaptiveCard
                    .Setup(x => x.Update(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string templateJson, string dataJson, string state) => new ProviderOperationResult(ProviderOperationStatus.Success, null, null, null));

               extensionAdaptiveCard
                    .Setup(x => x.State)
                    .Returns("VmCredential");

               credentialsCardSession.Initialize(extensionAdaptiveCard.Object);
               var op = credentialsCardSession.OnAction(@"{ ""Type"": ""Action.Execute"", ""Id"": ""okAction"" }", @"{ ""id"": ""okAction"", ""UserVal"": """", ""PassVal"": """" }");
               await op.AsTask();
            }
            else if (actionRequired.CorrectiveActionCardSession is WaitForLoginAdaptiveCardSession waitForLoginCardSession)
            {
                var extensionAdaptiveCard = new Mock<IExtensionAdaptiveCard>();
                extensionAdaptiveCard
                    .Setup(x => x.Update(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string templateJson, string dataJson, string state) => new ProviderOperationResult(ProviderOperationStatus.Success, null, null, null));

                extensionAdaptiveCard
                    .Setup(x => x.State)
                    .Returns("WaitForVmUserLogin");

                // TODO: figure out how to wait for user's login
                waitForLoginCardSession.Initialize(extensionAdaptiveCard.Object);
                var op = waitForLoginCardSession.OnAction(@"{ ""Type"": ""Action.Execute"", ""Id"": ""okAction"" }", @"{ }");
                await op.AsTask();
            }
        };

        var result = await operation.StartAsync();
        if (result != null)
        {
            operationData.ConfigurationResult = result;
        }

        Assert.IsNotNull(operationData.ConfigurationResult);
        PrintResultData(operationData.ConfigurationResult);

        Assert.IsTrue(operationData.ConfigurationResult.Result.Status == ProviderOperationStatus.Success);
        Assert.IsNotNull(operationData.ConfigurationResult.OpenConfigurationSetResult);
        Assert.IsNull(operationData.ConfigurationResult.OpenConfigurationSetResult.ResultCode);
        Assert.IsNotNull(operationData.ConfigurationResult.ApplyConfigurationSetResult);
        Assert.IsNull(operationData.ConfigurationResult.ApplyConfigurationSetResult.ResultCode);
        Assert.IsNotNull(operationData.ConfigurationResult.ApplyConfigurationSetResult.UnitResults);
        Assert.AreEqual(operationData.ConfigurationResult.ApplyConfigurationSetResult.UnitResults.Count, 2);

        foreach (var unitResult in operationData.ConfigurationResult.ApplyConfigurationSetResult.UnitResults)
        {
            // Assert.AreEqual(unitResult.Unit.Type, "");
            // Assert.AreEqual(unitResult.Unit.Identifier, "");
            // Assert.AreEqual(unitResult.Unit.IsGroup, "");
            // TODO: This state remains "Unknown", need to investigate why.
            // Assert.AreEqual(unitResult.Unit.State, ConfigurationUnitState.Completed);
            Assert.IsNull(unitResult.ResultInformation.ResultCode);
        }
    }

    private void PrintResultData(ApplyConfigurationResult configurationResult)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"Result:\n");
        sb.Append(CultureInfo.InvariantCulture, $"  ProviderOperation result status: {configurationResult.Result.Status}\n");
        sb.Append(CultureInfo.InvariantCulture, $"  ProviderOperation diagnostic text: {configurationResult.Result.DiagnosticText}\n");

        if (configurationResult.OpenConfigurationSetResult != null)
        {
            sb.Append(CultureInfo.InvariantCulture, $"  OpenConfigurationSetResult:\n");
            sb.Append(CultureInfo.InvariantCulture, $"    ResultCode: {configurationResult.OpenConfigurationSetResult.ResultCode}\n");
            sb.Append(CultureInfo.InvariantCulture, $"    Field: {configurationResult.OpenConfigurationSetResult.Field}\n");
            sb.Append(CultureInfo.InvariantCulture, $"    Value: {configurationResult.OpenConfigurationSetResult.Value}\n");
            sb.Append(CultureInfo.InvariantCulture, $"    Line: {configurationResult.OpenConfigurationSetResult.Line}\n");
            sb.Append(CultureInfo.InvariantCulture, $"    Column: {configurationResult.OpenConfigurationSetResult.Column}\n");
        }
        else
        {
            sb.Append(CultureInfo.InvariantCulture, $"  OpenConfigurationSetResult: null\n");
        }

        if (configurationResult.ApplyConfigurationSetResult != null)
        {
            sb.Append(CultureInfo.InvariantCulture, $"  ApplyConfigurationSetResult:\n");
            sb.Append(CultureInfo.InvariantCulture, $"    ResultCode: {configurationResult.ApplyConfigurationSetResult.ResultCode}\n");

            if (configurationResult.ApplyConfigurationSetResult.UnitResults != null)
            {
                sb.Append(CultureInfo.InvariantCulture, $"    UnitResults:\n");
                foreach (var unitResult in configurationResult.ApplyConfigurationSetResult.UnitResults)
                {
                    if (unitResult.Unit != null)
                    {
                        sb.Append(CultureInfo.InvariantCulture, $"      Unit:\n");
                        sb.Append(CultureInfo.InvariantCulture, $"        Type: {unitResult.Unit.Type}\n");
                        sb.Append(CultureInfo.InvariantCulture, $"        Identifier: {unitResult.Unit.Identifier}\n");
                        sb.Append(CultureInfo.InvariantCulture, $"        State: {unitResult.Unit.State}\n");
                        sb.Append(CultureInfo.InvariantCulture, $"        IsGroup: {unitResult.Unit.IsGroup}\n");
                        sb.Append(CultureInfo.InvariantCulture, $"        Units: {unitResult.Unit.Units}\n");
                    }
                    else
                    {
                        sb.Append(CultureInfo.InvariantCulture, $"      Unit: null\n");
                    }

                    sb.Append(CultureInfo.InvariantCulture, $"      PreviouslyInDesiredState: {unitResult.PreviouslyInDesiredState}\n");
                    sb.Append(CultureInfo.InvariantCulture, $"      RebootRequired: {unitResult.RebootRequired}\n");

                    if (unitResult.ResultInformation != null)
                    {
                        sb.Append(CultureInfo.InvariantCulture, $"      ResultInformation: \n");
                        sb.Append(CultureInfo.InvariantCulture, $"        ResultCode: {unitResult.ResultInformation.ResultCode}\n");
                        sb.Append(CultureInfo.InvariantCulture, $"        Description: {unitResult.ResultInformation.Description}\n");
                        sb.Append(CultureInfo.InvariantCulture, $"        Details: {unitResult.ResultInformation.Details}\n");
                        sb.Append(CultureInfo.InvariantCulture, $"        ResultSource: {unitResult.ResultInformation.ResultSource}\n");
                    }
                    else
                    {
                        sb.Append(CultureInfo.InvariantCulture, $"      ResultInformation: null\n");
                    }
                }
            }
            else
            {
                sb.Append(CultureInfo.InvariantCulture, $"    UnitResults: null\n");
            }
        }
        else
        {
            sb.Append(CultureInfo.InvariantCulture, $"  ApplyConfigurationSetResult: null\n");
        }

        System.Diagnostics.Trace.WriteLine(sb);
    }

    private void PrintProgressData(ConfigurationSetChangeData progressData)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(CultureInfo.InvariantCulture, $"Progress:\n");
        sb.Append(CultureInfo.InvariantCulture, $"  Change: {progressData.Change}\n");
        sb.Append(CultureInfo.InvariantCulture, $"  SetState: {progressData.SetState}\n");
        sb.Append(CultureInfo.InvariantCulture, $"  UnitState: {progressData.UnitState}\n");

        if (progressData.ResultInformation != null)
        {
            sb.Append(CultureInfo.InvariantCulture, $"  ResultInformation: \n");
            sb.Append(CultureInfo.InvariantCulture, $"    ResultCode: {progressData.ResultInformation.ResultCode}\n");
            sb.Append(CultureInfo.InvariantCulture, $"    Description: {progressData.ResultInformation.Description}\n");
            sb.Append(CultureInfo.InvariantCulture, $"    Details: {progressData.ResultInformation.Details}\n");
            sb.Append(CultureInfo.InvariantCulture, $"    ResultSource: {progressData.ResultInformation.ResultSource}\n");
        }
        else
        {
            sb.Append(CultureInfo.InvariantCulture, $"  ResultInformation: null\n");
        }

        if (progressData.Unit != null)
        {
            sb.Append(CultureInfo.InvariantCulture, $"  ConfigurationUnit: \n");
            sb.Append(CultureInfo.InvariantCulture, $"    Type: {progressData.Unit.Type}\n");
            sb.Append(CultureInfo.InvariantCulture, $"    Identifier: {progressData.Unit.Identifier}\n");
            sb.Append(CultureInfo.InvariantCulture, $"    State: {progressData.Unit.State}\n");
            sb.Append(CultureInfo.InvariantCulture, $"    IsGroup: {progressData.Unit.IsGroup}\n");
            sb.Append(CultureInfo.InvariantCulture, $"    Units: {progressData.Unit.Units}\n");
        }
        else
        {
            sb.Append(CultureInfo.InvariantCulture, $"  ConfigurationUnit: null\n");
        }

        System.Diagnostics.Trace.WriteLine(sb);
    }

    [TestMethod]
    public void TestDevSetupAgentDeployment()
    {
        IHyperVManager hyperVManager = TestHost!.GetService<IHyperVManager>();
        var machines = hyperVManager.GetAllVirtualMachines();
        HyperVVirtualMachine? testVm = null;
        foreach (var vm in machines)
        {
            if (string.Equals(vm.DisplayName, "TestVM", StringComparison.OrdinalIgnoreCase))
            {
                testVm = vm;
                break;
            }
        }

        Assert.IsNotNull(testVm);

        var powerShell = TestHost!.GetService<IPowerShellService>();

        var deploymentHelperMock = new Mock<DevSetupAgentDeploymentHelper>(powerShell, testVm.Id);
        deploymentHelperMock.CallBase = true;
        deploymentHelperMock.Setup(x => x.GetSourcePath(It.IsAny<ushort>())).Returns(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\DevSetupAgent.zip"));

        // TODO: figure out how to get the password from the user
        var userName = string.Empty;
        var pwd = new NetworkCredential(string.Empty, string.Empty).SecurePassword;
        deploymentHelperMock.Object.DeployDevSetupAgent(userName, pwd);

        var session = deploymentHelperMock.Object.GetSessionObject(new PSCredential(userName, pwd));

        // Verify that the DevSetupAgent service is installed and running in the VM.
        var getService = new StatementBuilder()
               .AddCommand("Invoke-Command")
               .AddParameter("Session", session)
               .AddParameter("ScriptBlock", ScriptBlock.Create("Get-Service DevSetupAgent"))
               .Build();

        var result = powerShell!.Execute(getService, PipeType.None);
        Assert.IsTrue(string.IsNullOrEmpty(result.CommandOutputErrorMessage));

        var psObject = result.PsObjects.FirstOrDefault();
        Assert.IsNotNull(psObject);
        Assert.AreEqual(psObject!.Properties["Status"].Value, "Running");
    }

    /// <summary>
    /// This test will attempt to create a new VM on the computer using the VMGalleryService.
    /// It should be ran normally, not in a CI/CD pipeline until we have pipeline machines
    /// that have Hyper-V enabled and running.
    /// </summary>
    [TestMethod]
    public async Task TestVirtualMachineCreationFromVmGallery()
    {
        var vmGalleryService = TestHost!.GetService<IVMGalleryService>();
        var hyperVProvider = TestHost!.GetService<IComputeSystemProvider>();
        var expectedVMName = "New Windows 11 VM for Integration test";
        var imageList = await vmGalleryService.GetGalleryImagesAsync();
        var smallestImageIndex = await GetIndexOfImageWithSmallestRequiredSpace(imageList);
        var inputJson = JsonSerializer.Serialize(new VMGalleryCreationUserInput()
        {
            NewEnvironmentName = expectedVMName,

            // Get Image with the smallest size from gallery, we'll use it to create a VM.
            SelectedImageListIndex = smallestImageIndex,
        });

        var createComputeSystemOperation = hyperVProvider.CreateCreateComputeSystemOperation(null, inputJson);
        createComputeSystemOperation!.Progress += OnProgressReceived;

        // Act
        var createComputeSystemResult = await createComputeSystemOperation!.StartAsync();
        createComputeSystemOperation!.Progress -= OnProgressReceived;

        // Assert
        Assert.AreEqual(ProviderOperationStatus.Success, createComputeSystemResult.Result.Status);
        Assert.AreEqual(expectedVMName, createComputeSystemResult.ComputeSystem.DisplayName);
        CleanUpVirtualMachine(createComputeSystemResult.ComputeSystem, imageList.Images[smallestImageIndex], expectedVMName);
    }

    public void CleanUpVirtualMachine(IComputeSystem computeSystem, VMGalleryImage image, string virtualMachineName)
    {
        try
        {
            var hyperVManager = TestHost!.GetService<IHyperVManager>();
            var virtualMachine = computeSystem as HyperVVirtualMachine;
            var temp = Path.GetTempPath();
            var archiveFileExtension = Path.GetExtension(image.Disk.ArchiveRelativePath);
            var fileName = image.Disk.Hash.Split(":").Last();
            var archiveFilePath = Path.Combine(temp, $"{fileName}.{archiveFileExtension}");

            try
            {
                // remove extracted VHD file
                var hardDisk = virtualMachine!.GetHardDrives().First();
                File.Delete(hardDisk!.Path!);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to stop virtual machine with name VHD from default Hyper-V path: {ex}");
            }

            try
            {
                // remove archive file
                File.Delete(archiveFilePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to remove archive file from {temp}  {virtualMachineName}: {ex}");
            }

            // remove virtual machine
            hyperVManager.RemoveVirtualMachine(Guid.Parse(computeSystem.Id));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to clean up virtual machine with name {virtualMachineName}: {ex}");
        }
    }

    public async Task<int> GetIndexOfImageWithSmallestRequiredSpace(VMGalleryImageList imageList)
    {
        var smallestImageIndex = 0;
        var smallestSize = ulong.MaxValue;
        var httpClient = TestHost!.GetService<IHttpClientFactory>()!.CreateClient();
        for (var i = 0; i < imageList.Images.Count; i++)
        {
            var sourceWebUri = new Uri(imageList.Images[i].Disk.Uri);
            var response = await httpClient.GetAsync(sourceWebUri, HttpCompletionOption.ResponseHeadersRead);
            var totalSizeOfDisk = response.Content.Headers.ContentLength ?? 0L;
            if (ulong.TryParse(imageList.Images[i].Requirements.DiskSpace, CultureInfo.InvariantCulture, out var requiredDiskSpace))
            {
                // The Hype-V Quick Create feature in the Hyper-V Manager in Windows uses the size of the archive file and the size of the required disk space
                // value in the VM gallery Json to determin the size of the download. We'll use the same logic here to determine the smallest size.
                // I'm not sure why this is done in this way, but we'll do the same here. In the Quick Create window the 'Download' text shows the size of both
                // the archive file to be downloaded and the required disk space value added together.
                if ((requiredDiskSpace + (ulong)totalSizeOfDisk) < smallestSize)
                {
                    smallestSize = requiredDiskSpace + (ulong)totalSizeOfDisk;
                    smallestImageIndex = i;
                }
            }
        }

        return smallestImageIndex;
    }

    public void OnProgressReceived(ICreateComputeSystemOperation operation, CreateComputeSystemProgressEventArgs progressArgs)
    {
        lock (_testFileWriteLock)
        {
            // Only write to the file if the percentage has changed
            if (progressArgs.PercentageCompleted == PreviousPercentage)
            {
                return;
            }

            var temp = Path.GetTempPath();
            var progressFilePath = Path.Combine(temp, $"{tempFileNameToWriteProgress}.txt");
            var progressData = $"{progressArgs.Status}: percentage: {progressArgs.PercentageCompleted}%";
            PreviousPercentage = progressArgs.PercentageCompleted;

            // Write the string array to a new file named "HyperVVMCreationProgress.txt".
            if (!Path.Exists(progressFilePath))
            {
                using var newFile = new FileStream(progressFilePath, FileMode.Create, FileAccess.ReadWrite);
                using var writer = new StreamWriter(newFile);
                writer.WriteLine(progressData);
            }
            else
            {
                using var outputFile = File.AppendText(progressFilePath);
                outputFile.WriteLine(progressData);
            }
        }
    }
}
