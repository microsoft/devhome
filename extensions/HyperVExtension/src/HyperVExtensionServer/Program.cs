// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Common.Extensions;
using HyperVExtension.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.AppLifecycle;
using Serilog;
using Windows.ApplicationModel.Activation;

namespace HyperVExtension;

public sealed class Program
{
    public static IHost? Host
    {
        get; set;
    }

    [MTAThread]
    public static async Task Main([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] string[] args)
    {
        // Set up Logging
        Environment.SetEnvironmentVariable("DEVHOME_LOGS_ROOT", Path.Join(Helpers.Logging.LogFolderRoot, "HyperV"));
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings_hyperv.json")
            .Build();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        Log.Information($"Launched with args: {string.Join(' ', args.ToArray())}");

        // Force the app to be single instanced.
        // Get or register the main instance.
        var mainInstance = AppInstance.FindOrRegisterForKey("mainInstance");
        var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        if (!mainInstance.IsCurrent)
        {
            Log.Information($"Not main instance, redirecting.");
            await mainInstance.RedirectActivationToAsync(activationArgs);
            Log.CloseAndFlush();
            return;
        }

        // Build the host container before handling activation.
        BuildHostContainer();

        // Register for activation redirection.
        AppInstance.GetCurrent().Activated += AppActivationRedirected;

        if (args.Length > 0 && args[0] == "-RegisterProcessAsComServer")
        {
            HandleCOMServerActivation();
        }
        else
        {
            Log.Warning("Not being launched as a ComServer... exiting.");
        }

        Log.CloseAndFlush();
    }

    private static void AppActivationRedirected(object? sender, Microsoft.Windows.AppLifecycle.AppActivationArguments activationArgs)
    {
        Log.Information($"Redirected with kind: {activationArgs.Kind}");

        // Handle COM server.
        if (activationArgs.Kind == ExtendedActivationKind.Launch)
        {
            var launchActivatedEventArgs = activationArgs.Data as ILaunchActivatedEventArgs;
            var args = launchActivatedEventArgs?.Arguments.Split();

            if (args?.Length > 1 && args[1] == "-RegisterProcessAsComServer")
            {
                Log.Information($"Activation COM Registration Redirect: {string.Join(' ', args.ToList())}");
                HandleCOMServerActivation();
            }
        }

        // Handle Protocol.
        if (activationArgs.Kind == ExtendedActivationKind.Protocol)
        {
            var protocolActivatedEventArgs = activationArgs.Data as IProtocolActivatedEventArgs;
            if (protocolActivatedEventArgs is not null)
            {
                Log.Information($"Protocol Activation redirected from: {protocolActivatedEventArgs.Uri}");
                HandleProtocolActivation(protocolActivatedEventArgs.Uri);
            }
        }
    }

    private static void HandleProtocolActivation(Uri protocolUri)
    {
        // TODO: Handle protocol activation if need be.
    }

    private static void HandleCOMServerActivation()
    {
        Log.Information($"Activating COM Server");

        // Register and run COM server.
        // This could be called by either of the COM registrations, we will do them all to avoid deadlock and bind all on the extension's lifetime.
        using var extensionServer = new Microsoft.Windows.DevHome.SDK.ExtensionServer();
        var hyperVExtension = Host.GetService<HyperVExtension>();

        // We are instantiating extension instance once above, and returning it every time the callback in RegisterExtension below is called.
        // This makes sure that only one instance of the extension is alive, which is returned every time the host asks for the IExtension object.
        // If you want to instantiate a new instance each time the host asks, create the new instance inside the delegate.
        extensionServer.RegisterExtension(() => hyperVExtension, true);

        // This will make the main thread wait until the event is signaled by the extension class.
        // Since we have single instance of the extension object, we exit as soon as it is disposed.
        hyperVExtension.ExtensionDisposedEvent.WaitOne();
        Log.Information($"Extension is disposed.");
    }

    /// <summary>
    /// Creates the host container for the HyperVExtension server application. This can be used to register
    /// services and other dependencies throughout the application.
    /// </summary>
    private static void BuildHostContainer()
    {
        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        UseDefaultServiceProvider((context, options) =>
        {
            options.ValidateOnBuild = true;
        }).
        ConfigureServices((context, services) =>
        {
            // Services
            services.AddCommonProjectServices(context);
            services.AddHyperVExtensionServices(context);
        }).
        Build();
    }
}
