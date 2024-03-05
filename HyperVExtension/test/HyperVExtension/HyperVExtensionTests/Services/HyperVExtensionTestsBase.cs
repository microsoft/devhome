// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation;
using System.ServiceProcess;
using HyperVExtension.Common;
using HyperVExtension.Models;
using HyperVExtension.Providers;
using HyperVExtension.Services;
using HyperVExtension.UnitTest.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.SDK;
using Moq;
using Moq.Language;

namespace HyperVExtension.UnitTest.HyperVExtensionTests.Services;

/// <summary>
/// Base class that can be used to test services throughout the HyperV extension.
/// </summary>
public class HyperVExtensionTestsBase
{
    protected Mock<IStringResource>? MockedStringResource { get; set; }

    protected Mock<IPowerShellSession>? MockedPowerShellSession { get; set; }

    protected PSCustomObjectMock PowerShellHyperVModule { get; set; } = new() { Name = string.Empty };

    protected ServiceControllerStatus VirtualMachineManagementServiceStatus { get; set; } = ServiceControllerStatus.Running;

    protected IHost? TestHost { get; set; }

    [TestInitialize]
    public void TestInitialize()
    {
        MockedStringResource = new Mock<IStringResource>();
        MockedPowerShellSession = new Mock<IPowerShellSession>();
        TestHost = CreateTestHost();

        // Configure string resource localization to return the input key by default
        MockedStringResource
            .Setup(strResource => strResource.GetLocalized(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] args) => key);

        // setup the PoewrShell session for tests that don't produce an error.
        MockedPowerShellSession!
            .Setup(pss => pss.GetErrorMessages())
            .Returns(() => { return string.Empty; });
    }

    /// <summary>
    /// Gets a collection of PSObjects, that can be used to mock the collection of PSObjects returned
    /// MockedPowerShellSession.
    /// Use this when you need to mock functionality that uses the the PowerShell session.
    /// </summary>
    protected Collection<PSObject> CreatePSObjectCollection(object? mockedObject)
    {
        if (mockedObject == null)
        {
            // For cases where we want the PsObjects list to be empty;
            return new Collection<PSObject> { };
        }

        return new Collection<PSObject>
        {
            new(mockedObject),
        };
    }

    /// <summary>
    /// Sets up the PowerShellSession and returns an ISetupSequentialResult that derived classes can use
    /// to continue specifying a Collection<PSObject>  per 'Invoke' call to the PowerShellSession.
    /// </summary>
    protected ISetupSequentialResult<Collection<PSObject>> SetupPowerShellSessionInvokeResults()
    {
        // We Return the setup sequential result so other tests can add more ISetupSequentialResult's
        // to the setup for their individual test.
        return MockedPowerShellSession!
            .SetupSequence(pss => pss.Invoke());
    }

    /// <summary>
    /// Sets up the PowerShellSession Error messages and returns an ISetupSequentialResult that derived classes can use
    /// to continue specifying an error message values per 'Invoke' call to the PowerShellSession.
    /// </summary>
    protected ISetupSequentialResult<string> SetupPowerShellSessionErrorMessages()
    {
        // We Return the setup sequential result so other tests can add more ISetupSequentialResult's
        // to the setup for their individual test.
        return MockedPowerShellSession!
            .SetupSequence(pss => pss.GetErrorMessages());
    }

    protected void SetupHyperVTestMethod(string moduleName, ServiceControllerStatus status)
    {
        VirtualMachineManagementServiceStatus = status;
        PowerShellHyperVModule.Name = moduleName;
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
                services.AddSingleton<IStringResource>(MockedStringResource!.Object);
                services.AddSingleton<IComputeSystemProvider, HyperVProvider>();
                services.AddSingleton<HyperVExtension>();
                services.AddSingleton<IHyperVManager, HyperVManager>();
                services.AddSingleton<IWindowsIdentityService, WindowsIdentityServiceMock>();

                // Pattern to allow multiple non-service registered interfaces to be used with registered interfaces during construction.
                services.AddSingleton<IPowerShellService>(psService =>
                    ActivatorUtilities.CreateInstance<PowerShellService>(psService, MockedPowerShellSession!.Object));

                services.AddTransient<IWindowsServiceController>(controller =>
                    ActivatorUtilities.CreateInstance<WindowsServiceControllerMock>(controller, VirtualMachineManagementServiceStatus));

                services.AddSingleton<HyperVVirtualMachineFactory>(sp => psObject => ActivatorUtilities.CreateInstance<HyperVVirtualMachine>(sp, psObject));
            }).Build();
    }
}
