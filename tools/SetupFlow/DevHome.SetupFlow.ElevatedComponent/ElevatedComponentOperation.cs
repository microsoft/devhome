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
    /// <summary>
    /// Tasks arguments are passed to the elevated process as input at launch-time.
    /// </summary>
    /// <remarks>
    /// This object is used to ensures that the operations performed at runtime by the
    /// caller process were pre-approved.
    /// </remarks>
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
        // Ensure the package to install has been pre-approved by checking against the process tasks arguments
        var taskArgument = _tasksArguments.InstallPackages?.FirstOrDefault(def => def.PackageId == packageId && def.CatalogName == catalogName);
        if (taskArgument == null)
        {
            Log.Logger?.ReportError(Log.Component.Elevated, $"No match found for PackageId={packageId} and CatalogId={catalogName} in the process tasks arguments.");
            throw new ArgumentException($"Failed to install '{packageId}' from '{catalogName}' because it was not in the pre-approved list");
        }

        Log.Logger?.ReportInfo(Log.Component.Elevated, $"Installing package elevated: '{packageId}' from '{catalogName}'");
        var task = new ElevatedInstallTask();
        return task.InstallPackage(taskArgument.PackageId, taskArgument.CatalogName);
    }

    public int CreateDevDrive()
    {
        Log.Logger?.ReportInfo(Log.Component.Elevated, "Creating elevated Dev Drive storage operator");
        var task = new DevDriveStorageOperator();
        var taskArgument = _tasksArguments.CreateDevDrive;
        return task.CreateDevDrive(taskArgument.VirtDiskPath, taskArgument.SizeInBytes, taskArgument.NewDriveLetter, taskArgument.DriveLabel);
    }

    public IAsyncOperation<ElevatedConfigureTaskResult> ApplyConfigurationAsync()
    {
        Log.Logger?.ReportInfo(Log.Component.Elevated, "Applying DSC configuration elevated");
        var task = new ElevatedConfigurationTask();
        var taskArgument = _tasksArguments.Configure;
        return task.ApplyConfiguration(taskArgument.FilePath, taskArgument.Content);
    }
}
