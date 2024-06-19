// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common;
using DevHome.SetupFlow.Common.Elevation;
using DevHome.SetupFlow.ElevatedComponent;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DevHome.SetupFlow.ElevatedServer;

internal sealed class Program
{
    public static void Main(string[] args)
    {
        // Set up Logging
        Environment.SetEnvironmentVariable("DEVHOME_LOGS_ROOT", Path.Join(Logging.LogFolderRoot, "SetupFlowElevated"));
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings_setupflowelevated.json")
            .Build();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        if (args.Length <= 3)
        {
            Console.WriteLine("Called with wrong number of arguments");
            Environment.Exit(1);
            return;
        }

        var mappedFileName = args[0];
        var initEventName = args[1];
        var completionSemaphoreName = args[2];
        var tasksArgumentList = args.Skip(3).ToList();

        var operation = new ElevatedComponentOperation(tasksArgumentList);

        try
        {
            IPCSetup.CompleteRemoteObjectInitialization<IElevatedComponentOperation>(0, operation, mappedFileName, initEventName, completionSemaphoreName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Remote object initialization failed: {ex}");
            IPCSetup.CompleteRemoteObjectInitialization<IElevatedComponentOperation>(ex.HResult, null, mappedFileName, initEventName, completionSemaphoreName);
            throw;
        }
        finally
        {
            operation.Terminate();
            Log.Information("Terminating the setup flow elevated process");
            Log.CloseAndFlush();
        }
    }
}
