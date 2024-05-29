// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Net;
using System.ServiceProcess;
using HyperVExtension.Common;
using HyperVExtension.Models;
using HyperVExtension.Models.VirtualMachineCreation;
using HyperVExtension.Providers;
using HyperVExtension.Services;
using HyperVExtension.UnitTest.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.SDK;
using Moq;
using Moq.Language;
using Moq.Protected;

namespace HyperVExtension.UnitTest.HyperVExtensionTests.Services;

/// <summary>
/// Base class that can be used to test services throughout the HyperV extension.
/// </summary>
public class HyperVExtensionTestsBase : IDisposable
{
    private readonly Mock<IHttpClientFactory> mockFactory = new();

    private HttpClient mockHttpClient = new();

    public Mock<HttpMessageHandler> HttpHandler { get; set; } = new(MockBehavior.Strict);

    // Arbitrary 9 byte array to use for testing the retrieval of a byte array from the web.
    public byte[] GallerySymbolByteArray => new byte[9] { 137, 80, 78, 71, 13, 10, 26, 10, 0 };

    // hash of the Window 11 gallery disk object from our test gallery json below.
    // Note: this hash should be the name of the file  located in the HyperVExtension.UnitTest\Assets folder for testing purposes.
    // It is the actual sha256 hash of the zip file that contains a test virtual disk.
    public string GalleryDiskHash => "6CFDC8E5163679E32B9886CEEACEB95F8919B20799CA8E5A6207B9F72EFEFD40";

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

        // Create an HttpClient using the mocked handler
        mockHttpClient = new HttpClient(HttpHandler.Object);

        mockFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(mockHttpClient);
    }

    public void UpdateHttpClientResponseMock(List<HttpContent> returnList)
    {
        var handlerSequence = HttpHandler
           .Protected()
           .SetupSequence<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>());

        foreach (var item in returnList)
        {
            handlerSequence = AddResponse(handlerSequence, item);
        }
    }

    private ISetupSequentialResult<Task<HttpResponseMessage>> AddResponse(ISetupSequentialResult<Task<HttpResponseMessage>> handlerSequence, HttpContent content)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = content,
        };

        return handlerSequence.ReturnsAsync(response);
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
                services.AddSingleton<IHttpClientFactory>(mockFactory.Object);
                services.AddSingleton<HttpClient>(mockHttpClient);
                services.AddSingleton<IStringResource>(MockedStringResource!.Object);
                services.AddSingleton<IComputeSystemProvider, HyperVProvider>();
                services.AddSingleton<HyperVExtension>();
                services.AddSingleton<IHyperVManager, HyperVManager>();
                services.AddSingleton<IWindowsIdentityService, WindowsIdentityServiceMock>();
                services.AddSingleton<IVMGalleryService, VMGalleryService>();
                services.AddSingleton<IArchiveProviderFactory, ArchiveProviderFactory>();
                services.AddSingleton<IDownloaderService, DownloaderServiceMock>();

                // Pattern to allow multiple non-service registered interfaces to be used with registered interfaces during construction.
                services.AddSingleton<IPowerShellService>(psService =>
                    ActivatorUtilities.CreateInstance<PowerShellService>(psService, MockedPowerShellSession!.Object));

                services.AddTransient<IWindowsServiceController>(controller =>
                    ActivatorUtilities.CreateInstance<WindowsServiceControllerMock>(controller, VirtualMachineManagementServiceStatus));

                services.AddSingleton<HyperVVirtualMachineFactory>(serviceProvider => psObject => ActivatorUtilities.CreateInstance<HyperVVirtualMachine>(serviceProvider, psObject));
                services.AddSingleton<VmGalleryCreationOperationFactory>(serviceProvider => parameters => ActivatorUtilities.CreateInstance<VMGalleryVMCreationOperation>(serviceProvider, parameters));
            }).Build();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public void SetupGalleryHttpContent()
    {
        var contentList = new List<HttpContent>
        {
            new StringContent(TestGallery),
            new ByteArrayContent(GallerySymbolByteArray),
        };

        UpdateHttpClientResponseMock(contentList);
    }

    // This is a sample gallery json that is used to test the VMGalleryService.
    // The symbol hash is a sha256 hash of a 9 byte array specified in the _gallerySymbolByteArray so we can test it.
    // The disk hash is the sha256 hash of the zip file located in the 'HyperVExtension.UnitTest\Assets' folder.
    public string TestGallery => """
    {
        "images": [
        {
            "name": "Windows 11 dev environment",
            "publisher": "Microsoft",
            "lastUpdated": "2024-01-12T12:00:00Z",
            "version": "10.0.22621",
            "locale": "en-US",
            "description": [
            "This evaluation copy of Windows 11 21H2 will enable you to try out Windows 11 development with an evaluation copy of Windows.  ",
            "\r\n\r\n",
            "This evaluation copy will expire after a pre-determined amount of time.  ",
            "\r\n\r\n",
            "The license terms for the Windows 11 VMs supersede any conflicting Windows license terms.    ",
            "\r\n\r\n",
            "By using the virtual machine, you are accepting the EULAs for all the installed products.  ",
            "\r\n\r\n",
            "Please see https://aka.ms/windowsdevelopervirtualmachineeula for more information.  "
            ],
            "config": {
            "secureBoot": "true"
            },
            "requirements": {
            "diskSpace": "20000000000"
            },
            "disk": {
            "uri": "https://download.microsoft.com/download/f/4/f/f4f4b60a-1842-4666-8692-d03daa03f8f7/WinDev2401Eval.HyperV.zip",
            "hash": "sha256:6CFDC8E5163679E32B9886CEEACEB95F8919B20799CA8E5A6207B9F72EFEFD40",
            "archiveRelativePath": "WinDev2401Eval.vhdx"
            },
            "logo": {
            "uri": "https://download.microsoft.com/download/c/f/5/cf5b587c-98bf-4cfc-9844-a2a7d8c96d83/Windows11_Logo.png",
            "hash": "sha256:5E583CE95340BE9FF1CB56FD39C5DDDF3B3341B93E417144D361C3F29A5A7395"
            },
            "symbol": {
            "uri": "https://download.microsoft.com/download/c/f/5/cf5b587c-98bf-4cfc-9844-a2a7d8c96d83/Windows11_Symbol.png",
            "hash": "sha256:843AC23B1736B4487EC81CF7C07DDD9BB46AE5B7818C2C3843D99D62FA75F3C9"
            },
            "thumbnail": {
            "uri": "https://download.microsoft.com/download/c/f/5/cf5b587c-98bf-4cfc-9844-a2a7d8c96d83/Windows11_Thumbnail.png",
            "hash": "sha256:E7BF96E18754D71E41B32A8EDCDE9E2F1DBAF47C7CD05D6DE0764CD4E4EA5066"
            },
            "details": [
            {
                "name": "Edition",
                "value": "Windows 11 Enterprise"
            },
            {
                "name": "Copyright",
                "value": "Copyright (c) Microsoft Corporation. All rights reserved."
            },
            {
                "name": "License",
                "value": "By using the virtual machine, you are accepting the EULAs for all the installed products."
            }
            ]
        }
        ]
    }
    """;
}
