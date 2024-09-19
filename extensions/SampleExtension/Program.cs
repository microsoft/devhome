// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.SDK;
using SampleExtension.Providers;
using Serilog;

namespace SampleExtension;

public class Program
{
    [MTAThread]
    public static void Main([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] string[] args)
    {
        // Set up Logging
        Logging.SetupLogging("appsettings.json", "DevHomeSampleExtension");

        Log.Information($"Launched with args: {string.Join(' ', args.ToArray())}");

        if (args.Length > 0 && args[0] == "-RegisterProcessAsComServer")
        {
            using ExtensionServer server = new();
            var extensionDisposedEvent = new ManualResetEvent(false);

            // Create host with dependency injection
            using var host = CreateHost();
            var extensionInstance = new SampleExtension(extensionDisposedEvent, host);

            // We are instantiating an extension instance once above, and returning it every time the callback in RegisterExtension below is called.
            // This makes sure that only one instance of SampleExtension is alive, which is returned every time the host asks for the IExtension object.
            // If you want to instantiate a new instance each time the host asks, create the new instance inside the delegate.
            server.RegisterExtension(() => extensionInstance, true);

            Log.Information("Extension Started");

            // This will make the main thread wait until the event is signaled by the extension class.
            // Since we have single instance of the extension object, we exit as soon as it is disposed.
            extensionDisposedEvent.WaitOne();
        }
        else
        {
            Log.Information("Not being launched as a Extension... exiting.");
        }

        Log.CloseAndFlush();
    }

    private static IHost CreateHost()
    {
        var host = Host.
            CreateDefaultBuilder().
            UseContentRoot(AppContext.BaseDirectory).
            UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateOnBuild = true;
            }).
            ConfigureServices((context, services) =>
            {
                // Logging
                services.AddLogging(builder => builder.AddSerilog(dispose: true));
            }).
        Build();

        Log.Information("Services Host creation successful");
        return host;
    }
}
