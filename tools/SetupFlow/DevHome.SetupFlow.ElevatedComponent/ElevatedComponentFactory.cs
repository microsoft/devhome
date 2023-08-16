// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using DevHome.SetupFlow.Common.Contracts;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.ElevatedComponent.Helpers;
using DevHome.SetupFlow.ElevatedComponent.Tasks;
using Windows.Foundation;

namespace DevHome.SetupFlow.ElevatedComponent;

/// <summary>
/// Factory for objects that run in the elevated background process.
/// </summary>
public sealed class ElevatedComponentFactory : IElevatedComponentFactory
{
    private readonly Dictionary<Guid, ITaskDefinition> _taskDefinitions = new ();

    public ElevatedComponentFactory(string taskDefinitionString)
    {
        var tasksDefinition = TasksDefinition.FromJsonString(taskDefinitionString) ?? new TasksDefinition();
        foreach (var task in tasksDefinition.Install)
        {
            _taskDefinitions.Add(task.TaskId, task);
        }
    }

    public void WriteToStdOut(string value)
    {
        Console.WriteLine(value);
    }

    public ElevatedInstallTask CreateElevatedInstallTask()
    {
        Log.Logger?.ReportInfo(Log.Component.Elevated, "Creating elevated package installer");
        return new ElevatedInstallTask();
    }

    public DevDriveStorageOperator CreateDevDriveStorageOperator()
    {
        Log.Logger?.ReportInfo(Log.Component.Elevated, "Creating elevated Dev Drive storage operator");
        return new DevDriveStorageOperator();
    }

    public ElevatedConfigurationTask CreateElevatedConfigurationTask()
    {
        Log.Logger?.ReportInfo(Log.Component.Elevated, "Creating elevated Configuration File applier");
        return new ElevatedConfigurationTask();
    }

    public IAsyncOperation<ElevatedInstallTaskResult> ExecuteInstallTask(Guid taskId)
    {
        Log.Logger?.ReportInfo(Log.Component.Elevated, "Creating elevated package installer");
        var installTask = new ElevatedInstallTask();
        var installTaskDefinition = GetTask<InstallTaskDefinition>(taskId);
        return installTask.InstallPackage(installTaskDefinition.PackageId, installTaskDefinition.CatalogName);
    }

    private T GetTask<T>(Guid id)
    {
        if (_taskDefinitions.TryGetValue(id, out var task) && task is T tTask)
        {
            return tTask;
        }

        throw new ArgumentException($"{id} of type {typeof(T)} was not found");
    }
}
