// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

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
    private readonly TasksDefinition _tasksDefinition;

    public ElevatedComponentFactory(string[] args, int index)
    {
        _tasksDefinition = TasksDefinition.FromCliArgument(args, index);
    }

    public void WriteToStdOut(string value)
    {
        Console.WriteLine(value);
    }

    public IAsyncOperation<ElevatedInstallTaskResult> InstallPackage()
    {
        Log.Logger?.ReportInfo(Log.Component.Elevated, "Creating elevated package installer");
        var task = new ElevatedInstallTask();
        var taskDefinition = _tasksDefinition.Install[0];
        return task.InstallPackage(taskDefinition.PackageId, taskDefinition.CatalogName);
    }

    public int CreateDevDrive()
    {
        Log.Logger?.ReportInfo(Log.Component.Elevated, "Creating elevated Dev Drive storage operator");
        var task = new DevDriveStorageOperator();
        var taskDefinition = _tasksDefinition.DevDrive;
        return task.CreateDevDrive(taskDefinition.VirtDiskPath, taskDefinition.SizeInBytes, taskDefinition.NewDriveLetter, taskDefinition.DriveLabel);
    }

    public IAsyncOperation<ElevatedConfigureTaskResult> ApplyConfiguration()
    {
        return Task.Run(async () =>
        {
            Log.Logger?.ReportInfo(Log.Component.Elevated, "Creating elevated Configuration File applier");
            var task = new ElevatedConfigurationTask();
            var taskDefinition = _tasksDefinition.Configuration;
            return await task.ApplyConfiguration(taskDefinition.FilePath, taskDefinition.Content);
        }).AsAsyncOperation();
    }
}
