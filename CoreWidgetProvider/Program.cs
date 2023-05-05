// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CoreWidgetProvider.Helpers;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;

namespace CoreWidgetProvider;
public sealed class Program
{
    [MTAThread]
    public static void Main([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] string[] args)
    {
        Log.Logger()?.ReportInfo($"Launched with args: {string.Join(' ', args.ToArray())}");

        // TODO: Do we need a lock here to prevent race condition around MainInstance registered?

        // Force the app to be single instanced
        // Get or register the main instance
        var mainInstance = AppInstance.FindOrRegisterForKey("mainInstance");
        var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

        // If the main instance isn't this current instance
        if (!mainInstance.IsCurrent)
        {
            Log.Logger()?.ReportInfo($"Not main instance, redirecting.");
            mainInstance.RedirectActivationToAsync(activationArgs).AsTask().Wait();
            return;
        }

        // Otherwise, we're in the main instance
        // Register for activation redirection
        AppInstance.GetCurrent().Activated += AppActivationRedirected;

        if (args.Length > 0 && args[0] == "-RegisterProcessAsComServer")
        {
            HandleCOMServerActivation();
        }
        else
        {
            Log.Logger()?.ReportWarn("Not being launched as a ComServer... exiting.");
        }

        Log.Logger()?.Dispose();
    }

    private static void AppActivationRedirected(object? sender, Microsoft.Windows.AppLifecycle.AppActivationArguments activationArgs)
    {
        Log.Logger()?.ReportInfo($"Redirected with kind: {activationArgs.Kind}");

        // Handle COM server
        if (activationArgs.Kind == ExtendedActivationKind.Launch)
        {
            var d = activationArgs.Data as ILaunchActivatedEventArgs;
            var args = d?.Arguments.Split();

            if (args?.Length > 0 && args[1] == "-RegisterProcessAsComServer")
            {
                Log.Logger()?.ReportInfo($"Activation COM Registration Redirect: {string.Join(' ', args.ToList())}");
                HandleCOMServerActivation();
            }
        }
    }

    private static void HandleCOMServerActivation()
    {
        Log.Logger()?.ReportInfo($"Activating COM Server");
        var pluginDisposedEvent = new ManualResetEvent(false);

        // Do Widget COM server registration
        // We are not using a disposed event for this, as we want the widgets to be disposed when the plugin is disposed.
        using var widgetServer = new Widgets.WidgetServer(pluginDisposedEvent);
        var widgetProviderInstance = new Widgets.WidgetProvider();
        widgetServer.RegisterWidget(() => widgetProviderInstance);

        pluginDisposedEvent.WaitOne();
        Log.Logger()?.ReportInfo($"Widget server is disposed.");
    }
}
