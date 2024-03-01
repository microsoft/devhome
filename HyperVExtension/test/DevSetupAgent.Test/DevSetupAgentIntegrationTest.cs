// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using HyperVExtension.DevSetupAgent;
using HyperVExtension.HostGuestCommunication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using Windows.UI.Accessibility;

namespace DevSetupAgent.Test;

[TestClass]
public class DevSetupAgentIntegrationTest
{
    protected IHost TestHost
    {
        get; set;
    }

    public DevSetupAgentIntegrationTest()
    {
        TestHost = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<DevAgentService>();
                services.AddSingleton<IRequestFactory, RequestFactory>();
                services.AddSingleton<IRegistryChannelSettings, TestRegistryChannelSettings>();
                services.AddSingleton<IHostChannel, HostRegistryChannel>();
                services.AddSingleton<IRequestManager, RequestManager>();
            }).Build();
    }

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext)
    {
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        //// Registry.CurrentUser.DeleteSubKeyTree(@"TEST", false);
    }

    [TestInitialize]
    public void TestInitialize()
    {
        TestHost.GetService<DevAgentService>().StartAsync(CancellationToken.None);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        TestHost.GetService<DevAgentService>().StopAsync(CancellationToken.None).Wait();
    }

    [TestMethod]
    public void TestGetVersionRequest()
    {
        var registryChannelSettings = TestHost.GetService<IRegistryChannelSettings>();
        var inputkey = Registry.CurrentUser.CreateSubKey(registryChannelSettings.FromHostRegistryKeyPath);
        var messageId = "DevSetup{10000000-1000-1000-1000-100000000000}";
        var messageName = messageId + "~1~1";
        inputkey.SetValue(messageName, $"{{\"RequestId\": \"{messageId}\", \"RequestType\": \"GetVersion\", \"Version\": 1, \"Timestamp\":\"2023-11-21T08:08:58.6287789Z\"}}");

        Thread.Sleep(3000);

        var outputKey = Registry.CurrentUser.CreateSubKey(registryChannelSettings.ToHostRegistryKeyPath);
        var responseMessage = (string?)outputKey.GetValue(messageName);
        Assert.IsNotNull(responseMessage);
        var json = JsonDocument.Parse(responseMessage).RootElement;
        Assert.AreEqual(messageId, json.GetProperty("RequestId").GetString());

        // Check that the timestamp is within 5 second of the current
        var time = json.GetProperty("Timestamp").GetDateTime();
        var now = DateTime.UtcNow;
        Assert.IsTrue(now - time < TimeSpan.FromSeconds(5));

        var version = json.GetProperty("Version").GetInt32();
        Assert.AreEqual(1, version);

        // TODO: Check that the response message is deleted
    }

    /// <summary>
    /// Test that a simple IsUserLoggedIn request can be sent to DevSetupAgent and that it responds
    /// with IsUserLoggedIn property set to true.
    /// Only works from elevated command prompt and LsaEnumerateLogonSessions requires Admin, System, or SeTcbPrivilege.
    /// </summary>
    [TestMethod]
    public void TestIsUserLoggedInRequest()
    {
        var registryChannelSettings = TestHost.GetService<IRegistryChannelSettings>();
        var inputkey = Registry.CurrentUser.CreateSubKey(registryChannelSettings.FromHostRegistryKeyPath);
        var messageId = "DevSetup{10000000-1000-1000-1000-100000000000}";
        var messageName = messageId + "~1~1";
        inputkey.SetValue(messageName, $"{{\"RequestId\": \"{messageId}\", \"RequestType\": \"IsUserLoggedIn\", \"Version\": 1, \"Timestamp\":\"2023-11-21T08:08:58.6287789Z\"}}");

        Thread.Sleep(3000);

        var outputKey = Registry.CurrentUser.CreateSubKey(registryChannelSettings.ToHostRegistryKeyPath);
        var responseMessage = (string?)outputKey.GetValue(messageName);
        Assert.IsNotNull(responseMessage);
        var json = JsonDocument.Parse(responseMessage).RootElement;
        Assert.AreEqual(messageId, json.GetProperty("RequestId").GetString());

        // Check that the timestamp is within 5 second of the current
        var time = json.GetProperty("Timestamp").GetDateTime();
        var now = DateTime.UtcNow;
        Assert.IsTrue(now - time < TimeSpan.FromSeconds(5));

        var isUserLoggedIn = json.GetProperty("IsUserLoggedIn").GetBoolean();
        Assert.AreEqual(true, isUserLoggedIn);
    }

    [TestMethod]
    public void TestInvalidRequest()
    {
        var registryChannelSettings = TestHost.GetService<IRegistryChannelSettings>();
        var inputkey = Registry.CurrentUser.CreateSubKey(registryChannelSettings.FromHostRegistryKeyPath);
        var messageId = "DevSetup{10000000-1000-1000-1000-200000000000}";
        var messageName = messageId + "~1~1";
        inputkey.SetValue(messageName, $"{{\"RequestId\": \"{messageId}\", \"Version\": 1, \"Timestamp\":\"2023-11-21T08:08:58.6287789Z\"}}");

        Thread.Sleep(3000);

        var outputKey = Registry.CurrentUser.CreateSubKey(registryChannelSettings.ToHostRegistryKeyPath);
        var responseMessage = (string?)outputKey.GetValue(messageName);
        Assert.IsNotNull(responseMessage);
        var json = JsonDocument.Parse(responseMessage).RootElement;
        Assert.AreEqual(messageId, json.GetProperty("RequestId").GetString());

        // Check that the timestamp is within 5 second of the current
        var time = json.GetProperty("Timestamp").GetDateTime();
        var now = DateTime.UtcNow;
        Assert.IsTrue(now - time < TimeSpan.FromSeconds(5));

        var status = json.GetProperty("Status").GetUInt32();
        Assert.AreNotEqual(0, status);
    }

    /// <summary>
    /// Test that a simple Configure request can be sent to DevSetupEngine and that it responds with
    /// Progress and Completed results.
    /// Currently DevSetupEngine needs to be started manually from command line for this test.
    /// </summary>
    [TestMethod]
    public void TestConfigureRequest()
    {
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

        var noNewLinesYaml = yaml.Replace(System.Environment.NewLine, "\\n");

        var registryChannelSettings = TestHost.GetService<IRegistryChannelSettings>();
        var inputkey = Registry.CurrentUser.CreateSubKey(registryChannelSettings.FromHostRegistryKeyPath);
        var messageId = "DevSetup{10000000-1000-1000-1000-100000000001}";
        var messageName = messageId + "~1~1";
        var requestData =
            $"{{\"RequestId\": \"{messageId}\"," +
            $" \"RequestType\": \"Configure\", \"Version\": 1, \"Timestamp\":\"2023-11-21T08:08:58.6287789Z\"," +
            $" \"Configure\": \"{noNewLinesYaml}\" }}";

        inputkey.SetValue(messageName, requestData);

        var outputKey = Registry.CurrentUser.CreateSubKey(registryChannelSettings.ToHostRegistryKeyPath);
        var waitTime = DateTime.Now + TimeSpan.FromMinutes(3);
        var foundProgressMessage = false;
        var foundCompletedMessage = false;
        while ((waitTime > DateTime.Now) && !foundCompletedMessage)
        {
            Thread.Sleep(1000);
            var messages = MessageHelper.MergeMessageParts(MessageHelper.GetRegistryMessageKvp(outputKey));
            if (messages.Count == 0)
            {
                continue;
            }

            foreach (var message in messages)
            {
                System.Diagnostics.Trace.WriteLine($"Found response registry value '{message.Key}'");

                var responseMessage = message.Value;
                if (responseMessage != null)
                {
                    var json = JsonDocument.Parse(responseMessage).RootElement;
                    Assert.AreEqual(messageId, json.GetProperty("RequestId").GetString());

                    var responseType = json.GetProperty("ResponseType").GetString();
                    if (responseType == "Completed")
                    {
                        var applyConfigurationResult = json.GetProperty("ApplyConfigurationResult").GetString();
                        Assert.IsNotNull(applyConfigurationResult);
                        System.Diagnostics.Trace.WriteLine(applyConfigurationResult);
                        foundCompletedMessage = true;
                    }
                    else if (responseType == "Progress")
                    {
                        var configurationSetChangeData = json.GetProperty("ConfigurationSetChangeData").GetString();
                        Assert.IsNotNull(configurationSetChangeData);
                        System.Diagnostics.Trace.WriteLine(configurationSetChangeData);
                        foundProgressMessage = true;
                    }
                    else
                    {
                        Assert.Fail($"Unexpected response type: {responseType}");
                    }
                }

                MessageHelper.DeleteAllMessages(Registry.CurrentUser, registryChannelSettings.FromHostRegistryKeyPath, message.Key);
            }
        }

        Assert.IsNotNull(foundProgressMessage);
        Assert.IsNotNull(foundCompletedMessage);
    }
}
