// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
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
        var allTaskDefinitions = new List<ITaskDefinition>();
        allTaskDefinitions.AddRange(tasksDefinition.Install ?? new List<InstallTaskDefinition>());
        if (tasksDefinition.Configuration != null)
        {
            allTaskDefinitions.Add(tasksDefinition.Configuration);
        }

        if (tasksDefinition.DevDrive != null)
        {
            allTaskDefinitions.Add(tasksDefinition.DevDrive);
        }

        _taskDefinitions = allTaskDefinitions.ToDictionary(task => task.TaskId, task => task);
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

    public IAsyncOperation<ElevatedInstallTaskResult> InstallPackage(Guid taskId)
    {
        Log.Logger?.ReportInfo(Log.Component.Elevated, "Creating elevated package installer");
        var task = new ElevatedInstallTask();
        var taskDefinition = GetTask<InstallTaskDefinition>(taskId);
        return task.InstallPackage(taskDefinition.PackageId, taskDefinition.CatalogName);
    }

    public int CreateDevDrive(Guid taskId)
    {
        Log.Logger?.ReportInfo(Log.Component.Elevated, "Creating elevated Dev Drive storage operator");
        var task = new DevDriveStorageOperator();
        var taskDefinition = GetTask<DevDriveTaskDefinition>(taskId);
        return task.CreateDevDrive(taskDefinition.VirtDiskPath, taskDefinition.SizeInBytes, taskDefinition.NewDriveLetter, taskDefinition.DriveLabel);
    }

    public IAsyncOperation<ElevatedConfigureTaskResult> ApplyConfiguration(Guid taskId)
    {
        return Task.Run(async () =>
        {
            Log.Logger?.ReportInfo(Log.Component.Elevated, "Creating elevated Configuration File applier");
            var task = new ElevatedConfigurationTask();
            var taskDefinition = GetTask<ConfigurationTaskDefinition>(taskId);
            return await task.ApplyConfiguration(taskDefinition.FilePath, taskDefinition.Content);
        }).AsAsyncOperation();
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
