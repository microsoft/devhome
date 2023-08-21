// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.Common.Contracts;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.ElevatedComponent.Helpers;
using DevHome.SetupFlow.ElevatedComponent.Tasks;
using Windows.Foundation;

namespace DevHome.SetupFlow.ElevatedComponent;

/// <summary>
/// Class for executing operations in the elevated background process.
/// </summary>
public sealed class ElevatedComponentOperation : IElevatedComponentOperation
{
    private readonly TasksArguments _tasksArguments;

    public ElevatedComponentOperation(IList<string> tasksArgumentList)
    {
        _tasksArguments = TasksArguments.FromArgumentList(tasksArgumentList);
    }

    public void WriteToStdOut(string value)
    {
        Console.WriteLine(value);
    }

    public IAsyncOperation<ElevatedInstallTaskResult> InstallPackageAsync(string packageId, string catalogName)
    {
        var taskArgument = _tasksArguments.InstallPackages?.FirstOrDefault(def => def.PackageId == packageId && def.CatalogName == catalogName);
        if (taskArgument == null)
        {
            Log.Logger?.ReportError(Log.Component.Elevated, $"Failed to install '{packageId}' from '{catalogName}' because it was not found");
            throw new ArgumentException($"Package id {packageId} and/or catalog {catalogName} was not found");
        }

        Log.Logger?.ReportInfo(Log.Component.Elevated, $"Installing package elevated: '{packageId}' from '{catalogName}'");
        var task = new ElevatedInstallTask();
        return task.InstallPackage(taskArgument.PackageId, taskArgument.CatalogName);
    }

    public int CreateDevDrive()
    {
        Log.Logger?.ReportInfo(Log.Component.Elevated, "Creating elevated Dev Drive storage operator");
        var task = new DevDriveStorageOperator();
        var taskArgument = _tasksArguments.DevDrive;
        return task.CreateDevDrive(taskArgument.VirtDiskPath, taskArgument.SizeInBytes, taskArgument.NewDriveLetter, taskArgument.DriveLabel);
    }

    public IAsyncOperation<ElevatedConfigureTaskResult> ApplyConfigurationAsync()
    {
        Log.Logger?.ReportInfo(Log.Component.Elevated, "Applying DSC configuration elevated");
        var task = new ElevatedConfigurationTask();
        var taskArgument = _tasksArguments.Configuration;
        return task.ApplyConfiguration(taskArgument.FilePath, taskArgument.Content);
    }
}
