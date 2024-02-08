// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using HyperVExtension.DevSetupEngine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HyperVExtension.DevSetupEngine;

internal class Program
{
    public static IHost? Host
    {
        get; set;
    }

    [MTAThread]
    public static void Main([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] string[] args)
    {
        Logging.Logger()?.ReportInfo($"Launched with args: {string.Join(' ', args.ToArray())}");

        BuildHostContainer();

        if ((args.Length > 0) && (args[0] == "-RegisterProcessAsComServer"))
        {
            RegisterProcessAsComServer();
        }
        else if ((args.Length > 0) && (args[0] == "-RegisterComServer"))
        {
            RegisterComServer();
        }
        else
        {
            Logging.Logger()?.ReportWarn("Unknown arguments... exiting.");
        }
    }

    private static void RegisterProcessAsComServer()
    {
        Logging.Logger()?.ReportInfo($"Activating COM Server");

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
        Logging.Logger()?.ReportInfo($"Extension is disposed.");
    }

    private static void RegisterComServer()
    {
        // TODO: Register COM Server in HKLM.
        Logging.Logger()?.ReportInfo($"TODO: Register COM Server in HKLM");
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
