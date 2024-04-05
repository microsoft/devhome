// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using HyperVExtension.DevSetupAgent;
using Serilog;
using Windows.Win32;
using Windows.Win32.Security;
using Windows.Win32.System.Com;

unsafe
{
    // TODO: Set real security descriptor to allow access from System+Admns+Interactive Users
    var hr = PInvoke.CoInitializeSecurity(
                new(null),
                -1,
                null,
                null,
                RPC_C_AUTHN_LEVEL.RPC_C_AUTHN_LEVEL_DEFAULT,
                RPC_C_IMP_LEVEL.RPC_C_IMP_LEVEL_IDENTIFY,
                null,
                EOLE_AUTHENTICATION_CAPABILITIES.EOAC_NONE);

    if (hr < 0)
    {
        Marshal.ThrowExceptionForHR(hr);
    }
}

// Set up Logging
Environment.SetEnvironmentVariable("DEVHOME_LOGS_ROOT", Path.Join(HyperVExtension.DevSetupEngine.Logging.LogFolderRoot, "HyperV"));
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings_hypervsetupagent.json")
    .Build();
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "DevAgent";
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<DevAgentService>();
        services.AddSingleton<DevAgentService>();
        services.AddSingleton<IRequestFactory, RequestFactory>();
        services.AddSingleton<IRegistryChannelSettings, RegistryChannelSettings>();
        services.AddSingleton<IHostChannel, HostRegistryChannel>();
        services.AddSingleton<IRequestManager, RequestManager>();
    })
    .Build();

host.Run();
Log.CloseAndFlush();
