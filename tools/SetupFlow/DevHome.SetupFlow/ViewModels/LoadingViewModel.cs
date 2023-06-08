// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

extern alias Projection;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.Contracts.Services;
using DevHome.SetupFlow.Common.Elevation;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Projection::DevHome.SetupFlow.ElevatedComponent;
using WinUIEx;

namespace DevHome.SetupFlow.ViewModels;

public partial class LoadingViewModel : SetupPageViewModelBase
{
    private readonly IHost _host;

    private readonly ElementTheme _currentTheme;

    private static readonly BitmapImage DarkCaution = new (new Uri("ms-appx:///DevHome.SetupFlow/Assets/DarkCaution.png"));
    private static readonly BitmapImage DarkError = new (new Uri("ms-appx:///DevHome.SetupFlow/Assets/DarkError.png"));
    private static readonly BitmapImage DarkSuccess = new (new Uri("ms-appx:///DevHome.SetupFlow/Assets/DarkSuccess.png"));
    private static readonly BitmapImage LightCaution = new (new Uri("ms-appx:///DevHome.SetupFlow/Assets/LightCaution.png"));
    private static readonly BitmapImage LightError = new (new Uri("ms-appx:///DevHome.SetupFlow/Assets/LightError.png"));
    private static readonly BitmapImage LightSuccess = new (new Uri("ms-appx:///DevHome.SetupFlow/Assets/LightSuccess.png"));

#pragma warning disable SA1310 // Field names should not contain underscore
    private const int NUMBER_OF_PARALLEL_RUNNING_TASKS = 5;
#pragma warning restore SA1310 // Field names should not contain underscore

#pragma warning disable SA1310 // Field names should not contain underscore
    private const int MAX_RETRIES = 1;
#pragma warning restore SA1310 // Field names should not contain underscore

    private int _retryCount;

    /// <summary>
    /// Event raised when the execution of all tasks is completed.
    /// </summary>
    public event EventHandler ExecutionFinished;

    /// <summary>
    /// Keep track of all failed tasks so they can be re-ran if the user wishes.
    /// </summary>
    private readonly IList<TaskInformation> _failedTasks;

    /// <summary>
    /// All the tasks that will be executed.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<TaskInformation> _tasksToRun;

    /// <summary>
    /// List of all messages that shows up in the "action center" of the loading screen.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ActionCenterMessages> _actionCenterItems;

    /// <summary>
    /// Keep track of all tasks completed to show to the user.
    /// </summary>
    [ObservableProperty]
    private int _tasksCompleted;

    /// <summary>
    /// Number of tasks that were started for the "execution task #/#
    /// </summary>
    [ObservableProperty]
    private int _tasksStarted;

    /// <summary>
    /// Used to tell the user the number of tasks completed successfully.
    /// Used in the action center.
    /// </summary>
    [ObservableProperty]
    private int _tasksFinishedSuccessfully;

    /// <summary>
    /// Used in the action center to notify the user the number of tasks that failed.
    /// </summary>
    [ObservableProperty]
    private int _tasksFailed;

    /// <summary>
    /// Used in the UI to show the user how many tasks have been executed.
    /// </summary>
    [ObservableProperty]
    private string _executingTasks;

    /// <summary>
    /// Used in the UI to tell the user the number of tasks that failed and succeeded.
    /// </summary>
    [ObservableProperty]
    private string _actionCenterDisplay;

    /// <summary>
    /// Controls if the UI for "Restart all tasks" and "Continue to summary" are shown.
    /// </summary>
    [ObservableProperty]
    private Visibility _showRetryButton;

    /// <summary>
    /// Controls if the banner that notifies the user they've ran out of re-tries should be shown.
    /// </summary>
    [ObservableProperty]
    private bool _showOutOfRetriesBanner;

    /// <summary>
    /// Hides the banner telling the user "You used up all your re-tries"
    /// </summary>
    [RelayCommand]
    public void HideMaxRetryBanner()
    {
        ShowOutOfRetriesBanner = false;
    }

    /// <summary>
    /// Command to re-run all tasks by moving them from _failedTasks to TasksToRun
    /// </summary>
    [RelayCommand]
    public async void RestartFailedTasks()
    {
        TelemetryFactory.Get<ITelemetry>().LogMeasure("Loading_RestartFailedTasks_Event");
        Log.Logger?.ReportInfo(Log.Component.Loading, "Restarting all failed tasks");

        // Keep the number of successful tasks and needs attention tasks the same.
        // Change failed tasks to 0 because, once restarted, all tasks haven't failed yet.
        TasksStarted = 0;
        TasksFailed = 0;
        SetExecutingTaskAndActionCenter();
        TasksToRun = new ObservableCollection<TaskInformation>(_failedTasks);

        // Empty out the collection since all failed tasks are being re-ran
        _retryCount++;
        _failedTasks.Clear();
        ActionCenterItems = new ObservableCollection<ActionCenterMessages>();
        ShowRetryButton = Visibility.Collapsed;
        await StartAllTasks(TasksToRun);
    }

    /// <summary>
    /// Signals that execution is finished so the stepper can go to the summary page.
    /// </summary>
    [RelayCommand]
    public void GoToSummaryPage()
    {
        ExecutionFinished.Invoke(null, null);
    }

    public LoadingViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        IHost host)
        : base(stringResource, orchestrator)
    {
        _host = host;
        _tasksToRun = new ();

        // Assuming that the theme can't change while the user is in the loading screen.
        _currentTheme = Application.Current.GetService<IThemeSelectorService>().Theme;

        IsStepPage = false;
        IsNavigationBarVisible = false;
        NextPageButtonText = stringResource.GetLocalized(StringResourceKey.LoadingScreenGoToSummaryButtonContent);
        ShowRetryButton = Visibility.Collapsed;
        _failedTasks = new List<TaskInformation>();
        ActionCenterItems = new ();
    }

    /// <summary>
    /// Reads from the orchestrator to get all the tasks to run.
    /// The ordering of the tasks, ordering for dependencies, is done later.
    /// </summary>
    private void FetchTaskInformation()
    {
        Log.Logger?.ReportDebug(Log.Component.Loading, "Fetching task information");
        var taskIndex = 0;
        foreach (var taskGroup in Orchestrator.TaskGroups)
        {
            foreach (var task in taskGroup.SetupTasks)
            {
                TasksToRun.Add(new TaskInformation { TaskIndex = taskIndex++, TaskToExecute = task, MessageToShow = task.GetLoadingMessages().Executing, StatusIconGridVisibility = false });
            }
        }

        SetExecutingTaskAndActionCenter();
    }

    /// <summary>
    /// Simple method to change the ExecutingTasks and ActionCenter messages.
    /// </summary>
    private void SetExecutingTaskAndActionCenter()
    {
        ExecutingTasks = StringResource.GetLocalized(StringResourceKey.LoadingExecutingProgress, TasksStarted, TasksToRun.Count);
        ActionCenterDisplay = StringResource.GetLocalized(StringResourceKey.ActionCenterDisplay, 0);
    }

    /// <summary>
    /// Changes the internals of information according to the taskFinishedState.
    /// </summary>
    /// <param name="information">The information that will change.</param>
    /// <param name="taskFinishedState">The status of the task.</param>
    /// <remarks>
    /// TaskInformation is an ObservableObject inside an ObservableCollection.  Any changes to information
    /// will change the UI.
    /// </remarks>
    public void ChangeMessage(TaskInformation information, TaskFinishedState taskFinishedState)
    {
        Log.Logger?.ReportDebug(Log.Component.Loading, $"Updating message for task {information.MessageToShow} with state {taskFinishedState}");
        var stringToReplace = string.Empty;
        BitmapImage statusSymbolIcon = null;

        if (taskFinishedState == TaskFinishedState.Success)
        {
            if (information.TaskToExecute.RequiresReboot)
            {
                Log.Logger?.ReportDebug(Log.Component.Loading, "Task succeeded but requires reboot; adding to action center");
                stringToReplace = information.TaskToExecute.GetLoadingMessages().NeedsReboot;

                if (_currentTheme == ElementTheme.Dark)
                {
                    statusSymbolIcon = DarkCaution;
                }
                else
                {
                    statusSymbolIcon = LightCaution;
                }

                ActionCenterItems.Add(information.TaskToExecute.GetRebootMessage());
            }
            else
            {
                Log.Logger?.ReportDebug(Log.Component.Loading, "Task succeeded");
                stringToReplace = information.TaskToExecute.GetLoadingMessages().Finished;

                if (_currentTheme == ElementTheme.Dark)
                {
                    statusSymbolIcon = DarkSuccess;
                }
                else
                {
                    statusSymbolIcon = LightSuccess;
                }
            }

            TasksFinishedSuccessfully++;
        }
        else if (taskFinishedState == TaskFinishedState.Failure)
        {
            Log.Logger?.ReportDebug(Log.Component.Loading, "Task failed");
            stringToReplace = information.TaskToExecute.GetLoadingMessages().Error;
            if (_currentTheme == ElementTheme.Dark)
            {
                statusSymbolIcon = DarkError;
            }
            else
            {
                statusSymbolIcon = LightError;
            }

            ActionCenterItems.Add(information.TaskToExecute.GetErrorMessages());
            TasksFailed++;

            Log.Logger?.ReportDebug(Log.Component.Loading, "Adding task to list for retry");

            information.StatusIconGridVisibility = false;
            _failedTasks.Add(information);
        }

        information.StatusIconGridVisibility = true;
        information.StatusSymbolIcon = statusSymbolIcon;
        information.MessageToShow = stringToReplace;
    }

    /// <summary>
    /// Get all information needed to run all tasks and run them.
    /// </summary>
    protected async override Task OnFirstNavigateToAsync()
    {
        var isAdminRequired = Orchestrator.TaskGroups.Any(taskGroup => taskGroup.SetupTasks.Any(task => task.RequiresAdmin));
        if (isAdminRequired)
        {
            try
            {
                Orchestrator.RemoteElevatedFactory = await IPCSetup.CreateOutOfProcessObjectAsync<IElevatedComponentFactory>();
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.Loading, $"Failed to initialize elevated process.", e);
                Log.Logger?.ReportInfo(Log.Component.Loading, "Will continue with setup as best-effort");
            }
        }

        FetchTaskInformation();

        await StartAllTasks(TasksToRun);
    }

    /// <summary>
    /// Starts all the tasks in the passed in ObservableCollection.
    /// </summary>
    /// <param name="tasks">All the tasks to start</param>
    /// <returns>An awaitable task</returns>
    private async Task StartAllTasks(ObservableCollection<TaskInformation> tasks)
    {
        Log.Logger?.ReportInfo(Log.Component.Loading, "Starting all tasks");
        var window = Application.Current.GetService<WindowEx>();
        await Task.Run(async () =>
        {
            var tasksToRunFirst = new List<TaskInformation>();
            var tasksToRunSecond = new List<TaskInformation>();

            // TODO: Most likely need a better way to figure out dependencies.
            // https://github.com/microsoft/devhome/issues/627
            // However, right now, the only dependency is "does this need to wait for a dev drive"
            foreach (var taskInformation in tasks)
            {
                if (taskInformation.TaskToExecute.DependsOnDevDriveToBeInstalled)
                {
                    tasksToRunSecond.Add(taskInformation);
                }
                else
                {
                    tasksToRunFirst.Add(taskInformation);
                }
            }

            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = NUMBER_OF_PARALLEL_RUNNING_TASKS,
            };

            // Run all tasks that don't need dev drive installed.
            await Parallel.ForEachAsync(tasksToRunFirst, options, async (taskInformation, token) =>
            {
                await StartTaskAndReportResult(window, taskInformation);
            });

            // Run all the tasks that need dev drive installed.
            await Parallel.ForEachAsync(tasksToRunSecond, options, async (taskInformation, token) =>
            {
                await StartTaskAndReportResult(window, taskInformation);
            });
        });

        // All the tasks are done.  Re-try logic follows.
        if (!_failedTasks.Any())
        {
            Log.Logger?.ReportInfo(Log.Component.Loading, "All tasks succeeded.  Moving to next page");
            ExecutionFinished.Invoke(null, null);
        }
        else if (_retryCount >= MAX_RETRIES)
        {
            Log.Logger?.ReportInfo(Log.Component.Loading, "Max number of retries reached; moving to next page");
            ShowOutOfRetriesBanner = true;
            ShowRetryButton = Visibility.Collapsed;
        }
        else
        {
            Log.Logger?.ReportInfo(Log.Component.Loading, "Some tasks failed; showing retry button");

            // At this point some tasks ran into an error.
            // Give the user the option to re try them all or move to the next screen.
            ShowRetryButton = Visibility.Visible;
            IsNavigationBarVisible = true;
        }

        if (_failedTasks.Any())
        {
            TelemetryFactory.Get<ITelemetry>().Log("Loading_FailedTasks_Event", LogLevel.Measure, new LoadingRetryEvent(_failedTasks.Count));
        }
    }

    /// <summary>
    /// Runs the specified task and updates the UI when the task is finished.
    /// </summary>
    /// <param name="window">Used to get access to the dispatcher queue.</param>
    /// <param name="taskInformation">Information about the task to execute.  Will be modified</param>
    /// <returns>An awaitable task</returns>
    private async Task StartTaskAndReportResult(WinUIEx.WindowEx window, TaskInformation taskInformation)
    {
        // Start the task and wait for it to complete.
        try
        {
            window.DispatcherQueue.TryEnqueue(() =>
            {
                TasksStarted++;
                ExecutingTasks = StringResource.GetLocalized(StringResourceKey.LoadingExecutingProgress, TasksStarted, TasksToRun.Count);
            });

            TaskFinishedState taskFinishedState;
            if (taskInformation.TaskToExecute.RequiresAdmin && Orchestrator.RemoteElevatedFactory != null)
            {
                Log.Logger?.ReportInfo(Log.Component.Loading, "Starting task as admin");
                taskFinishedState = await taskInformation.TaskToExecute.ExecuteAsAdmin(Orchestrator.RemoteElevatedFactory.Value);
            }
            else
            {
                taskFinishedState = await taskInformation.TaskToExecute.Execute();
            }

            window.DispatcherQueue.TryEnqueue(() =>
            {
                ChangeMessage(taskInformation, taskFinishedState);

                TasksCompleted++;
                ActionCenterDisplay = StringResource.GetLocalized(StringResourceKey.ActionCenterDisplay, TasksFailed);
            });
        }
        catch
        {
            // Don't let a single task break everything
            // TODO: Show failed tasks on UI
            // https://github.com/microsoft/devhome/issues/629
        }
    }
}
