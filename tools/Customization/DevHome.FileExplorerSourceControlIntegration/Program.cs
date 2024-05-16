// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Windows.AppLifecycle;
using Serilog;
using Windows.ApplicationModel.Activation;

namespace FileExplorerSourceControlIntegration;

public sealed class Program
{
    [MTAThread]
    public static void Main([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] string[] args)
    {
        // Set up Logging
        Environment.SetEnvironmentVariable("DEVHOME_LOGS_ROOT", Path.Join(DevHome.Common.Logging.LogFolderRoot, "FileExplorerSourceControlIntegration"));
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings_FileExplorerSourceControl.json")
            .Build();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        Log.Information($"Launched with args: {string.Join(' ', args.ToArray())}");

        if (args.Length > 0 && args[0] == "-RegisterProcessAsComServer")
        {
            HandleCOMServerActivation();
        }
        else
        {
            Log.Warning("Not being launched as a ComServer... exiting.");
        }
    }

    private static void AppActivationRedirected(object sender, Microsoft.Windows.AppLifecycle.AppActivationArguments activationArgs)
    {
        Log.Information($"Redirected with kind: {activationArgs.Kind}");

        // Handle COM server
        if (activationArgs.Kind == ExtendedActivationKind.Launch)
        {
            var d = activationArgs.Data as ILaunchActivatedEventArgs;
            var args = d?.Arguments.Split();

            if (args?.Length > 0 && args[1] == "-RegisterProcessAsComServer")
            {
                Log.Information($"Activation COM Registration Redirect: {string.Join(' ', args.ToList())}");
                HandleCOMServerActivation();
            }
        }
    }

    private static void HandleCOMServerActivation()
    {
        Log.Information($"Activating COM Server");
        using var sourceControlProviderServer = new SourceControlProviderServer();
        var sourceControlProviderInstance = new SourceControlProvider();
        var wrapper = new Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer.PerFolderRootSelectorWrapper(sourceControlProviderInstance);
        sourceControlProviderServer.RegisterSourceControlProviderServer(() => wrapper);
        sourceControlProviderServer.Run();
    }
}
