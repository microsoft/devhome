// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.DevSetupEngine;
using WinRT;

namespace DevSetupEngine.Test;

/// <summary>
/// These tests currently require the DevSetupEngine COM server to be running.
/// It can be started manually from the command line: "DevSetupEngine.exe -RegisterProcessAsComServer"
/// </summary>
[TestClass]
public class DevSetupEngineIntegrationTest
{
    protected IHost TestHost
    {
        get; set;
    }

    public DevSetupEngineIntegrationTest()
    {
        TestHost = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
            }).Build();
    }

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext)
    {
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
    }

    [TestInitialize]
    public void TestInitialize()
    {
    }

    [TestCleanup]
    public void TestCleanup()
    {
    }

    [TestMethod]
    public void TestDevSetupEngineCreation()
    {
        // DevSetupEngine needs to be started manually from command line in the test.
        var devSetupEnginePtr = IntPtr.Zero;
        try
        {
            var hr = PInvoke.CoCreateInstance(Guid.Parse("82E86C64-A8B9-44F9-9323-C37982F2D8BE"), null, CLSCTX.CLSCTX_LOCAL_SERVER, typeof(IDevSetupEngine).GUID, out var devSetupEngineObj);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            devSetupEnginePtr = Marshal.GetIUnknownForObject(devSetupEngineObj);

            var devSetupEngine = MarshalInterface<IDevSetupEngine>.FromAbi(devSetupEnginePtr);
            Assert.IsNotNull(devSetupEngine);
        }
        finally
        {
            if (devSetupEnginePtr != IntPtr.Zero)
            {
                Marshal.Release(devSetupEnginePtr);
            }
        }
    }

    [TestMethod]
    public void TestConfigureRequest()
    {
        // DevSetupEngine needs to be started manually from command line in the test.
        var devSetupEnginePtr = IntPtr.Zero;
        try
        {
            var hr = PInvoke.CoCreateInstance(Guid.Parse("82E86C64-A8B9-44F9-9323-C37982F2D8BE"), null, CLSCTX.CLSCTX_LOCAL_SERVER, typeof(IDevSetupEngine).GUID, out var devSetupEngineObj);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            devSetupEnginePtr = Marshal.GetIUnknownForObject(devSetupEngineObj);

            var yaml =
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

            Dictionary<string, Dictionary<ConfigurationUnitState, bool>> progressResults = new()
            {
                { "OsVersion", new Dictionary<ConfigurationUnitState, bool>() { { ConfigurationUnitState.Pending, false }, { ConfigurationUnitState.InProgress, false }, { ConfigurationUnitState.Completed, false } } },
                { "DeveloperMode", new Dictionary<ConfigurationUnitState, bool>() { { ConfigurationUnitState.Pending, false }, { ConfigurationUnitState.InProgress, false }, { ConfigurationUnitState.Completed, false } } },
            };

            var devSetupEngine = MarshalInterface<IDevSetupEngine>.FromAbi(devSetupEnginePtr);
            var operation = devSetupEngine.ApplyConfigurationAsync(yaml);

            operation.Progress = (operation, data) =>
            {
                System.Diagnostics.Trace.WriteLine($"  - Unit: {data.Unit.Type} [{data.UnitState}]");
                Assert.IsTrue(data.Change == ConfigurationSetChangeEventType.UnitStateChanged);
                progressResults[data.Unit.Type][data.UnitState] = true;
            };

            operation.AsTask().Wait();
            var result = operation.GetResults();

            Assert.IsTrue(result.OpenConfigurationSetResult != null);
            Assert.IsTrue(result.OpenConfigurationSetResult.ResultCode == null);

            Assert.IsTrue(result.ApplyConfigurationSetResult != null);
            Assert.IsTrue(result.ApplyConfigurationSetResult.ResultCode == null);

            for (var i = 0; i < result.ApplyConfigurationSetResult.UnitResults.Count; i++)
            {
                var unitResult = result.ApplyConfigurationSetResult.UnitResults[i];
                Assert.IsTrue(unitResult.ResultInformation.ResultCode == null);
                Assert.IsTrue(unitResult.RebootRequired == false);
            }

            foreach (var unitName in progressResults.Keys)
            {
                foreach (var unitState in progressResults[unitName].Keys)
                {
                    Assert.IsTrue(progressResults[unitName][unitState]);
                }
            }
        }
        catch (Exception ex)
        {
            Assert.Fail(ex.Message);
        }
        finally
        {
            if (devSetupEnginePtr != IntPtr.Zero)
            {
                Marshal.Release(devSetupEnginePtr);
            }
        }
    }
}
