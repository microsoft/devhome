// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.Common.Elevation;
using DevHome.SetupFlow.Contract.TaskOperator;
using DevHome.SetupFlow.TaskOperator;

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

        var factory = new TaskOperatorFactory();

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
}
