// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Common;
using HyperVExtension.Common.Extensions;
using HyperVExtension.Extensions;
using HyperVExtension.ExtensionServer;
using HyperVExtension.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Windows.ApplicationModel.Activation;
using Windows.Management.Deployment;

namespace HyperVExtension;

public sealed class Program
{
    public static IHost? Host
    {
        get; set;
    }

    [MTAThread]
    public static void Main([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] string[] args)
    {
        Logging.Logger()?.ReportInfo($"Launched with args: {string.Join(' ', args.ToArray())}");

        // Force the app to be single instanced.
        // Get or register the main instance.
        var mainInstance = AppInstance.FindOrRegisterForKey("mainInstance");
        var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        if (!mainInstance.IsCurrent)
        {
            Logging.Logger()?.ReportInfo($"Not main instance, redirecting.");
            mainInstance.RedirectActivationToAsync(activationArgs).AsTask().Wait();
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
            Logging.Logger()?.ReportWarn("Not being launched as a ComServer... exiting.");
        }

        Logging.Logger()?.Dispose();
    }

    private static void AppActivationRedirected(object? sender, Microsoft.Windows.AppLifecycle.AppActivationArguments activationArgs)
    {
        Logging.Logger()?.ReportInfo($"Redirected with kind: {activationArgs.Kind}");

        // Handle COM server.
        if (activationArgs.Kind == ExtendedActivationKind.Launch)
        {
            var launchActivatedEventArgs = activationArgs.Data as ILaunchActivatedEventArgs;
            var args = launchActivatedEventArgs?.Arguments.Split();

            if (args?.Length > 0 && args[1] == "-RegisterProcessAsComServer")
            {
                Logging.Logger()?.ReportInfo($"Activation COM Registration Redirect: {string.Join(' ', args.ToList())}");
                HandleCOMServerActivation();
            }
        }

        // Handle Protocol.
        if (activationArgs.Kind == ExtendedActivationKind.Protocol)
        {
            var protocolActivatedEventArgs = activationArgs.Data as IProtocolActivatedEventArgs;
            if (protocolActivatedEventArgs is not null)
            {
                Logging.Logger()?.ReportInfo($"Protocol Activation redirected from: {protocolActivatedEventArgs.Uri}");
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
        Logging.Logger()?.ReportInfo($"Activating COM Server");

        // Register and run COM server.
        // This could be called by either of the COM registrations, we will do them all to avoid deadlock and bind all on the extension's lifetime.
        using var extensionServer = new Microsoft.Windows.DevHome.SDK.ExtensionServer();
        var hyperVExtension = Host.GetService<HyperVExtension>();

        // We are instantiating extension instance once above, and returning it every time the callback in RegisterExtension below is called.
        // This makes sure that only one instance of the extension is alive, which is returned every time the host asks for the IExtension object.
        // If you want to instantiate a new instance each time the host asks, create the new instance inside the delegate.
        extensionServer.RegisterExtension(() => hyperVExtension, true);

        // This will make the main thread wait until the event is signalled by the extension class.
        // Since we have single instance of the extension object, we exit as soon as it is disposed.
        hyperVExtension.ExtensionDisposedEvent.WaitOne();
        Logging.Logger()?.ReportInfo($"Extension is disposed.");
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
