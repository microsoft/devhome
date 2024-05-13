// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Security.AccessControl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using Serilog;

namespace HyperVExtension.DevSetupEngine;

internal sealed class Program
{
    private const string AppIdPath = @"SOFTWARE\Classes\AppID\";
    private const string ClsIdIdPath = @"SOFTWARE\Classes\ClSID\";

    public static IHost? Host
    {
        get; set;
    }

    [MTAThread]
    public static int Main([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] string[] args)
    {
        // Set up Logging
        Environment.SetEnvironmentVariable("DEVHOME_LOGS_ROOT", Path.Join(Logging.LogFolderRoot, "HyperV"));
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings_hypervsetup.json")
            .Build();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information($"Launched with args: {string.Join(' ', args.ToArray())}");

            BuildHostContainer();

            if ((args.Length > 0) && string.Equals(args[0], "-RegisterProcessAsComServer", StringComparison.OrdinalIgnoreCase))
            {
                RegisterProcessAsComServer();
            }
            else if ((args.Length > 0) && string.Equals(args[0], "-RegisterComServer", StringComparison.OrdinalIgnoreCase))
            {
                RegisterComServer();
            }
            else if ((args.Length > 0) && string.Equals(args[0], "-UnregisterComServer", StringComparison.OrdinalIgnoreCase))
            {
                UnregisterComServer();
            }
            else
            {
                Log.Warning("Unknown arguments... exiting.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Exception: {ex}");
            Log.CloseAndFlush();
            return ex.HResult;
        }

        Log.CloseAndFlush();
        return 0;
    }

    private static void RegisterProcessAsComServer()
    {
        Log.Information($"Activating COM Server");

        // Register and run COM server.
        // This could be called by either of the COM registrations, we will do them all to avoid deadlock and bind all on the extension's lifetime.
        using var comServer = new ComServer();
        var devSetupEngine = Host!.GetService<DevSetupEngineImpl>();

        // We are instantiating extension instance once above, and returning it every time the callback in RegisterExtension below is called.
        // This makes sure that only one instance of the extension is alive, which is returned every time the host asks for the IExtension object.
        // If you want to instantiate a new instance each time the host asks, create the new instance inside the delegate.
        comServer.RegisterComServer(() => devSetupEngine);

        // This will make the main thread wait until the event is signaled by the extension class.
        // Since we have single instance of the extension object, we exit as soon as it is disposed.
        devSetupEngine.ComServerDisposedEvent.WaitOne();
        Log.Information($"Extension is disposed.");
    }

    private static void RegisterComServer()
    {
        var appId = typeof(DevSetupEngineImpl).GUID.ToString("B");

        var appIdKey = Registry.LocalMachine.CreateSubKey(AppIdPath + appId, true) ?? throw new Win32Exception();
        appIdKey.SetValue("RunAs", "Interactive User", RegistryValueKind.String);

        // O:PSG: BU Owner: principal self, Group: Built-in users
        // (A; ; 0xB; ; ; SY)      Allow SYSTEM
        // (A; ; 0xB; ; ; LS)      Allow Local Service
        // (A; ; 0xB; ; ; PS)      Allow Principal self
        // 0xB = (COM_RIGHTS_EXECUTE | COM_RIGHTS_EXECUTE_LOCAL | COM_RIGHTS_ACTIVATE_LOCAL
        var permissions = "O:PSG:BUD:(A;;0xB;;;SY)(A;;0xB;;;LS)(A;;0xB;;;PS)";
        RawSecurityDescriptor rawSd = new RawSecurityDescriptor(permissions);
        var sdBinaryForm = new byte[rawSd.BinaryLength];
        rawSd.GetBinaryForm(sdBinaryForm, 0);
        appIdKey.SetValue("AccessPermission", sdBinaryForm, RegistryValueKind.Binary);
        appIdKey.SetValue("LaunchPermission", sdBinaryForm, RegistryValueKind.Binary);

        var clsIdKey = Registry.LocalMachine.CreateSubKey(ClsIdIdPath + appId, true) ?? throw new Win32Exception();
        clsIdKey.SetValue("AppID", appId);

        var localServer32Key = clsIdKey.CreateSubKey("LocalServer32", true) ?? throw new Win32Exception();

        var exePath = Environment.ProcessPath!;

        localServer32Key.SetValue(string.Empty, "\"" + exePath + "\"" + " -RegisterProcessAsComServer");
        localServer32Key.SetValue("ServerExecutable", exePath);
    }

    private static void UnregisterComServer()
    {
        var appId = typeof(DevSetupEngineImpl).GUID.ToString("B");
        Registry.LocalMachine.DeleteSubKeyTree(AppIdPath + appId, false);
        Registry.LocalMachine.DeleteSubKeyTree(ClsIdIdPath + appId, false);
    }

    /// <summary>
    /// Creates the host container for the application. This can be used to register
    /// services and other dependencies throughout the application.
    /// </summary>
    private static void BuildHostContainer()
    {
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        UseDefaultServiceProvider((context, options) =>
        {
            options.ValidateOnBuild = true;
        }).
        ConfigureServices((context, services) =>
        {
            // Services
            services.AddSingleton<DevSetupEngineImpl>();
        }).
        Build();
    }
}
