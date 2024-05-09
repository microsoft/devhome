// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Runtime.InteropServices;
using HyperVExtension.DevSetupAgent;
using HyperVExtension.HostGuestCommunication;
using Serilog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Com;

// Set up Logging
Environment.SetEnvironmentVariable("DEVHOME_LOGS_ROOT", Path.Join(Logging.LogFolderRoot, "HyperV"));
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings_hypervsetupagent.json")
    .Build();
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

unsafe
{
    PSECURITY_DESCRIPTOR absolutSd = new(null);
    PSID ownerSid = new(null);
    PSID groupSid = new(null);
    ACL* dacl = default;
    ACL* sacl = default;

    try
    {
        // O:PSG:BU  Owner Principal Self, Group Built-in Users
        // (A;;0x3;;;SY)  Allow Local System
        // (A;;0x3;;;IU)  Allow Interactive User
        var accessPermission = "O:PSG:BUD:(A;;0x3;;;SY)(A;;0x3;;;IU)";
        uint securityDescriptorSize;
        PInvoke.ConvertStringSecurityDescriptorToSecurityDescriptor(accessPermission, PInvoke.SDDL_REVISION_1, out var securityDescriptor, &securityDescriptorSize);

        uint absoluteSdSize = default;
        uint daclSize = default;
        uint saclSize = default;
        uint ownerSize = default;
        uint groupSize = default;

        if (PInvoke.MakeAbsoluteSD(securityDescriptor, absolutSd, ref absoluteSdSize, null, ref daclSize, null, ref saclSize, ownerSid, ref ownerSize, groupSid, ref groupSize))
        {
            throw new HResultException(HRESULT.E_UNEXPECTED);
        }

        var error = Marshal.GetLastWin32Error();
        if (error != (int)WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
        {
            throw new Win32Exception(error);
        }

        absolutSd = new(PInvoke.LocalAlloc(Windows.Win32.System.Memory.LOCAL_ALLOC_FLAGS.LPTR, absoluteSdSize));
        dacl = (ACL*)PInvoke.LocalAlloc(Windows.Win32.System.Memory.LOCAL_ALLOC_FLAGS.LPTR, daclSize);
        sacl = (ACL*)PInvoke.LocalAlloc(Windows.Win32.System.Memory.LOCAL_ALLOC_FLAGS.LPTR, saclSize);
        ownerSid = new(PInvoke.LocalAlloc(Windows.Win32.System.Memory.LOCAL_ALLOC_FLAGS.LPTR, ownerSize));
        groupSid = new(PInvoke.LocalAlloc(Windows.Win32.System.Memory.LOCAL_ALLOC_FLAGS.LPTR, groupSize));

        if (!PInvoke.MakeAbsoluteSD(securityDescriptor, absolutSd, ref absoluteSdSize, dacl, ref daclSize, sacl, ref saclSize, ownerSid, ref ownerSize, groupSid, ref groupSize))
        {
            throw new HResultException(Marshal.GetLastWin32Error());
        }

        var hr = PInvoke.CoInitializeSecurity(
                    absolutSd,
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
    finally
    {
        if (sacl != default)
        {
            PInvoke.LocalFree((HLOCAL)sacl);
        }

        if (dacl != default)
        {
            PInvoke.LocalFree((HLOCAL)dacl);
        }

        if (groupSid != default)
        {
            PInvoke.LocalFree((HLOCAL)groupSid.Value);
        }

        if (ownerSid != default)
        {
            PInvoke.LocalFree((HLOCAL)ownerSid.Value);
        }
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
Log.CloseAndFlush();
