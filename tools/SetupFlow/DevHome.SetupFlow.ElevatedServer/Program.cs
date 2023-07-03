// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Services;
using DevHome.SetupFlow.Common.Elevation;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.Contract.TaskOperator;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskOperator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.ElevatedServer;

internal sealed class Program
{
    public static void Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Called with wrong number of arguments");
            Environment.Exit(1);
            return;
        }

        var mappedFileName = args[0];
        var initEventName = args[1];
        var completionSemaphoreName = args[2];

        var host = BuildHost();
        var factory = host.GetService<ITaskOperatorFactory>();

        try
        {
            IPCSetup.CompleteRemoteObjectInitialization<ITaskOperatorFactory>(0, factory, mappedFileName, initEventName, completionSemaphoreName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Remote factory initialization failed: {ex}");
            IPCSetup.CompleteRemoteObjectInitialization<ITaskOperatorFactory>(ex.HResult, null, mappedFileName, initEventName, completionSemaphoreName);
            throw;
        }
    }

    private static IHost BuildHost()
    {
        return Host.
            CreateDefaultBuilder().
            ConfigureServices((context, services) =>
            {
                services.AddSingleton<WindowsPackageManagerFactory, WindowsPackageManagerManualActivationFactory>();
                services.AddSingleton<IWindowsPackageManager, WindowsPackageManager>();
                services.AddSingleton<IPackageDeploymentService, PackageDeploymentService>();
                services.AddSingleton<IAppInstallManagerService, AppInstallManagerService>();
                services.AddSingleton<ITaskOperatorFactory, TaskOperatorFactory>();
            }).Build();
    }
}
