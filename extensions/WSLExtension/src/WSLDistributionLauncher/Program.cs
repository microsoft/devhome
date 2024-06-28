// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.IO.Pipes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.AppLifecycle;
using Serilog;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using WSLDistributionLauncher.Helpers;

namespace WSLDistributionLauncher;

public sealed class Program
{
    private static readonly WslLauncherFactory _launcherFactory = new();

    public static IHost? Host { get; set; }

    [MTAThread]
    public static void Main([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] string[] args)
    {
        // Set up Logging
        // Environment.SetEnvironmentVariable("DEVHOME_LOGS_ROOT", ApplicationData.Current.TemporaryFolder.Path);
        // var configuration = new ConfigurationBuilder().AddJsonFile("wsl_appsettings.json").Build();
        // Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();

        // Log.Information($"Launched with args: {string.Join(' ', args.ToArray())}");

        // Build the host container before handling activation.
        // BuildHostContainer();
        Console.WriteLine($"Launched with args: {string.Join(' ', args.ToArray())}");
        if (args.Length > 0 && args[0] == "-LaunchInteractive")
        {
            HandleWslDistroLaunch(args);
        }
        else
        {
            // /Log.Warning("Not being launched as a ComServer... exiting.");
        }

        while (true)
        {
        }

        // Log.CloseAndFlush();
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
            }).
            Build();
    }

    private static void HandleWslDistroLaunch(string[] args)
    {
        // Log.Information("Activating COM Server");
        try
        {
            _launcherFactory.GetLauncher(args).Launch();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            // Log.Error(ex, "Error launching WSL process");
        }

        // Log.Information("Extension is disposed.");
    }
}
