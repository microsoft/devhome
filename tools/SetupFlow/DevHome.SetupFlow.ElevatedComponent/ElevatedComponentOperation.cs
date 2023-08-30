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

    private readonly Dictionary<ITaskArguments, Operation> _operations = new ();
    private const int MaxRetryAttempts = 1;
    private readonly object _lock = new ();

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
        return Task.Run(async () =>
        {
            var taskArguments = GetInstallPackageTaskArguments(packageId, catalogName);
            ValidateAndBeginOperation(taskArguments);
            Log.Logger?.ReportInfo(Log.Component.Elevated, $"Installing package elevated: '{packageId}' from '{catalogName}'");
            var task = new ElevatedInstallTask();

            // Pass the pre-approved install package information as provided to the elevated process
            var result = await task.InstallPackage(taskArguments.PackageId, taskArguments.CatalogName);
            EndOperation(taskArguments, result.TaskSucceeded);
            return result;
        }).AsAsyncOperation();
    }

    public int CreateDevDrive()
    {
        var taskArguments = GetDevDriveTaskArguments();
        ValidateAndBeginOperation(taskArguments);
        Log.Logger?.ReportInfo(Log.Component.Elevated, "Creating elevated Dev Drive storage operator");
        var task = new DevDriveStorageOperator();

        // Pass the pre-approved DevDrive information as provided to the elevated process
        var result = task.CreateDevDrive(taskArguments.VirtDiskPath, taskArguments.SizeInBytes, taskArguments.NewDriveLetter, taskArguments.DriveLabel);
        EndOperation(taskArguments, result >= 0);

        return result;
    }

    public IAsyncOperation<ElevatedConfigureTaskResult> ApplyConfigurationAsync()
    {
        return Task.Run(async () =>
        {
            var taskArguments = GetConfigureTaskArguments();
            ValidateAndBeginOperation(taskArguments);
            Log.Logger?.ReportInfo(Log.Component.Elevated, "Applying DSC configuration elevated");
            var task = new ElevatedConfigurationTask();

            // Pass the pre-approved DSC configuration information as provided to the elevated process
            var result = await task.ApplyConfiguration(taskArguments.FilePath, taskArguments.Content);
            EndOperation(taskArguments, result.TaskSucceeded);
            return result;
        }).AsAsyncOperation();
    }

    private InstallPackageTaskArguments GetInstallPackageTaskArguments(string packageId, string catalogName)
    {
        // Ensure the package to install has been pre-approved by checking against the process tasks arguments
        var taskArguments = _tasksArguments.InstallPackages?.FirstOrDefault(def => def.PackageId == packageId && def.CatalogName == catalogName);
        if (taskArguments == null)
        {
            Log.Logger?.ReportError(Log.Component.Elevated, $"No match found for PackageId={packageId} and CatalogId={catalogName} in the process tasks arguments.");
            throw new ArgumentException($"Failed to install '{packageId}' from '{catalogName}' because it was not in the pre-approved tasks arguments");
        }

        return taskArguments;
    }

    private ConfigureTaskArguments GetConfigureTaskArguments()
    {
        var taskArguments = _tasksArguments.Configure;
        if (taskArguments == null)
        {
            throw new ArgumentException($"Failed to apply configuration because it was not in the pre-approved tasks arguments");
        }

        return taskArguments;
    }

    private CreateDevDriveTaskArguments GetDevDriveTaskArguments()
    {
        var taskArguments = _tasksArguments.CreateDevDrive;
        if (taskArguments == null)
        {
            throw new ArgumentException($"Failed to create a dev drive because it was not in the pre-approved tasks arguments");
        }

        return taskArguments;
    }

    /// <summary>
    /// Validate the execution of the operation with the provided tasks arguments
    /// </summary>
    /// <param name="taskArguments">Task arguments for the operation to execute</param>
    /// <exception cref="InvalidOperationException">Thrown if this operation cannot be performed</exception>
    private void ValidateAndBeginOperation(ITaskArguments taskArguments)
    {
        lock (_lock)
        {
            // Check if this operation has already been attempted before.
            if (_operations.TryGetValue(taskArguments, out var operation))
            {
                if (operation.InProgress)
                {
                    throw new InvalidOperationException($"Failed to perform operation because an identical operation is still executing.");
                }

                if (operation.RemainingAttempts <= 0)
                {
                    throw new InvalidOperationException($"Failed to perform operation because no more retry attempts are remaining.");
                }

                operation.RemainingAttempts--;
                operation.InProgress = true;
            }
            else
            {
                // This is the first time this operation is being executed
                _operations.Add(taskArguments, new Operation
                {
                    RemainingAttempts = MaxRetryAttempts,
                    InProgress = true,
                });
            }
        }
    }

    /// <summary>
    /// End operation with a boolean indicating whether it was successful or not.
    /// </summary>
    /// <param name="taskArguments">Task arguments </param>
    /// <param name="isSuccessful">Boolean indicating whether the operation was successful or not</param>
    private void EndOperation(ITaskArguments taskArguments, bool isSuccessful)
    {
        lock (_lock)
        {
            var operation = _operations[taskArguments];
            if (isSuccessful)
            {
                // Since the operation succeeded, prevent further attempts to
                // execute it again in the lifetime of the elevated process.
                operation.RemainingAttempts = 0;
            }

            operation.InProgress = false;
        }
    }

    private class Operation
    {
        public int RemainingAttempts { get; set; }

        public bool InProgress { get; set; }
    }
}
