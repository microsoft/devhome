// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using HyperVExtension.DevSetupAgent;
using Microsoft.Windows.DevHome.DevSetupEngine;
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
