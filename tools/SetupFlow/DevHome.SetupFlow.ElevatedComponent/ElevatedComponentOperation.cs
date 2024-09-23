// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using DevHome.Services.Core.Extensions;
using DevHome.Services.DesiredStateConfiguration.Extensions;
using DevHome.Services.WindowsPackageManager.Extensions;
using DevHome.SetupFlow.Common.Contracts;
using DevHome.SetupFlow.ElevatedComponent.Helpers;
using DevHome.SetupFlow.ElevatedComponent.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Windows.Foundation;

namespace DevHome.SetupFlow.ElevatedComponent;

/// <summary>
/// Class for executing operations in the elevated background process.
/// </summary>
public sealed class ElevatedComponentOperation : IElevatedComponentOperation
{
    private readonly Microsoft.Extensions.Logging.ILogger _logger;

    internal static IHost Host { get; } = BuildHost();

    /// <summary>
    /// Tasks arguments are passed to the elevated process as input at launch-time.
    /// </summary>
    /// <remarks>
    /// This object is used to ensure that the operations performed at runtime by the
    /// caller process were pre-approved.
    /// </remarks>
    private readonly TasksArguments _tasksArguments;

    /// <summary>
    /// Dictionary of operations state by task arguments.
    /// </summary>
    private readonly Dictionary<ITaskArguments, OperationState> _operationsState = new();
    private readonly object _operationStateLock = new();

    // TODO: Share this value with the caller process and make a configurable option
    // https://github.com/microsoft/devhome/issues/622
    private const int MaxRetryAttempts = 1;

    public ElevatedComponentOperation(IList<string> tasksArgumentList)
    {
        _logger = LoggerFactory.Create(lb => lb.AddSerilog(dispose: false)).CreateLogger<ElevatedComponentOperation>();

        try
        {
            _tasksArguments = TasksArguments.FromArgumentList(tasksArgumentList);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to parse tasks arguments");
            throw;
        }
    }

    public void WriteToStdOut(string value)
    {
        Console.WriteLine(value);
    }

    public IAsyncOperationWithProgress<ElevatedInstallTaskResult, Progress> InstallPackageAsync(string packageId, string catalogName, string version, Guid activityId)
    {
        var taskArguments = GetInstallPackageTaskArguments(packageId, catalogName, version);
        return AsyncInfo.Run<ElevatedInstallTaskResult, Progress>(async (token, progress) =>
        {
            return await ValidateAndExecuteAsync(
                taskArguments,
                async () =>
                {
                    _logger.LogInformation($"Installing package elevated: '{packageId}' from '{catalogName}'");
                    var task = new ElevatedInstallTask();
                    var install = task.InstallPackage(taskArguments.PackageId, taskArguments.CatalogName, version, activityId);
                    install.Progress += (_, p) =>
                    {
                        progress.Report(p);
                    };
                    return await install;
                },
                result => result.TaskSucceeded).AsAsyncOperation();
        });
    }

    public IAsyncOperation<int> CreateDevDriveAsync()
    {
        var taskArguments = GetDevDriveTaskArguments();

        // TODO: Return a result object instead of a primitive type
        // https://github.com/microsoft/devhome/issues/622
        return ValidateAndExecuteAsync(
            taskArguments,
            async () =>
            {
                _logger.LogInformation("Creating elevated Dev Drive storage operator");
                var task = new DevDriveStorageOperator();
                var result = task.CreateDevDrive(taskArguments.VirtDiskPath, taskArguments.SizeInBytes, taskArguments.NewDriveLetter, taskArguments.DriveLabel);
                return await Task.FromResult(result);
            },
            result => result >= 0).AsAsyncOperation();
    }

    public IAsyncOperation<ElevatedConfigureTaskResult> ApplyConfigurationAsync(Guid activityId)
    {
        var taskArguments = GetConfigureTaskArguments();
        return ValidateAndExecuteAsync(
            taskArguments,
            async () =>
            {
                _logger.LogInformation("Applying DSC configuration elevated");
                var task = new ElevatedConfigurationTask();
                return await task.ApplyConfiguration(taskArguments.FilePath, taskArguments.Content, activityId);
            },
            result => result.TaskSucceeded).AsAsyncOperation();
    }

    /// <summary>
    /// Terminate method to be called when the elevated process is shutting down.
    /// </summary>
    public void Terminate()
    {
        var allTasksArguments = _tasksArguments.GetAllTasksArguments();
        if (allTasksArguments.Count == _operationsState.Count)
        {
            _logger.LogInformation($"All operations for the tasks arguments provided to the elevated process were executed.");
        }
        else
        {
            // Check if any operation was never executed in the elevated process.
            foreach (var taskArguments in allTasksArguments)
            {
                if (!_operationsState.ContainsKey(taskArguments))
                {
                    _logger.LogWarning($"Operation for task arguments {string.Join(' ', taskArguments.ToArgumentList())} was provided to the elevated process but was never executed.");
                }
            }
        }
    }

    private InstallPackageTaskArguments GetInstallPackageTaskArguments(string packageId, string catalogName, string version)
    {
        // Ensure the package to install has been pre-approved by checking against the process tasks arguments
        var taskArguments = _tasksArguments.InstallPackages?.FirstOrDefault(def => def.PackageId == packageId && def.CatalogName == catalogName && def.Version == version);
        if (taskArguments == null)
        {
            _logger.LogError($"No match found for PackageId={packageId}, CatalogId={catalogName} and Version={version} in the process tasks arguments.");
            throw new ArgumentException($"Failed to install '{packageId}' ({version}) from '{catalogName}' because it was not in the pre-approved tasks arguments");
        }

        return taskArguments;
    }

    private ConfigureTaskArguments GetConfigureTaskArguments()
    {
        var taskArguments = _tasksArguments.Configure;
        if (taskArguments == null)
        {
            _logger.LogError($"No configuration task was found in the process tasks arguments ");
            throw new ArgumentException($"Failed to apply configuration because it was not in the pre-approved tasks arguments");
        }

        return taskArguments;
    }

    private CreateDevDriveTaskArguments GetDevDriveTaskArguments()
    {
        var taskArguments = _tasksArguments.CreateDevDrive;
        if (taskArguments == null)
        {
            _logger.LogError($"No 'create dev drive' task was found in the process tasks arguments ");
            throw new ArgumentException($"Failed to create a dev drive because it was not in the pre-approved tasks arguments");
        }

        return taskArguments;
    }

    /// <summary>
    /// Validate the execution of the operation with the provided tasks arguments and execute it.
    /// </summary>
    /// <param name="taskArguments">Task arguments for the operation to execute</param>
    /// <param name="executeFunction">Asynchronous execute operation function</param>
    /// <param name="resultProcessorFunction">Result processor function indicating whether the operation was successful or not</param>
    /// <returns>Result returned by the operation</returns>
    private async Task<TResult> ValidateAndExecuteAsync<TTaskArguments, TResult>(
        TTaskArguments taskArguments,
        Func<Task<TResult>> executeFunction,
        Func<TResult, bool> resultProcessorFunction)
        where TTaskArguments : ITaskArguments
    {
        try
        {
            ValidateAndBeginOperation(taskArguments);
            var result = await executeFunction();
            var success = resultProcessorFunction(result);
            EndOperation(taskArguments, success);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to validate or execute operation");
            EndOperation(taskArguments, false);
            throw;
        }
    }

    /// <summary>
    /// Validate the execution of the operation with the provided tasks arguments
    /// </summary>
    /// <param name="taskArguments">Task arguments for the operation to execute</param>
    /// <exception cref="InvalidOperationException">Thrown if this operation cannot be performed</exception>
    /// <remarks>Validates that only one operation for the given task arguments
    /// is executed at a time. Also prevents retrying an operation if no more
    /// attempts are left</remarks>
    private void ValidateAndBeginOperation(ITaskArguments taskArguments)
    {
        lock (_operationStateLock)
        {
            // Check if this operation has already been attempted before.
            if (_operationsState.TryGetValue(taskArguments, out var operationState))
            {
                if (operationState.InProgress)
                {
                    throw new InvalidOperationException($"Failed to perform operation because an identical operation is still executing.");
                }

                if (operationState.RemainingAttempts <= 0)
                {
                    throw new InvalidOperationException($"Failed to perform operation because the maximum number of attempts has been reached.");
                }

                operationState.RemainingAttempts--;
                operationState.InProgress = true;
            }
            else
            {
                // This is the first time this operation is being executed
                _operationsState.Add(taskArguments, new OperationState
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
        lock (_operationStateLock)
        {
            if (_operationsState.TryGetValue(taskArguments, out var operationState))
            {
                if (isSuccessful)
                {
                    // Since the operation succeeded, prevent further attempts to
                    // execute it again in the lifetime of the elevated process.
                    operationState.RemainingAttempts = 0;
                }

                operationState.InProgress = false;
            }
        }
    }

    /// <summary>
    /// Class for tracking the state of an operation.
    /// </summary>
    private sealed class OperationState
    {
        /// <summary>
        /// Gets or sets the number of remaining attempts to execute this operation.
        /// </summary>
        public int RemainingAttempts { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this operation is currently in progress.
        /// </summary>
        public bool InProgress { get; set; }
    }

    private static IHost BuildHost()
    {
        return Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateOnBuild = true;
            })
            .ConfigureServices((context, services) =>
            {
                // Add Serilog logging for ILogger.
                services.AddLogging(lb => lb.AddSerilog(dispose: true));

                // Service projects
                services.AddCore();
                services.AddWinGetElevated();
                services.AddDSC();
            })
            .Build();
    }
}
