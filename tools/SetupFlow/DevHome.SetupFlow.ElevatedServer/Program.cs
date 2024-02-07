// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.Common.Elevation;
using DevHome.SetupFlow.ElevatedComponent;

namespace DevHome.SetupFlow.ElevatedServer;

internal sealed class Program
{
    public static void Main(string[] args)
    {
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
        }
    }
}
