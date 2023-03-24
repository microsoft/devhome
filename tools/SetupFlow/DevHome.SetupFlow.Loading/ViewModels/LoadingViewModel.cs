// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.SetupFlow.Loading.Models;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Core;
using WinUIEx;

namespace DevHome.SetupFlow.Loading.ViewModels;

public partial class LoadingViewModel : SetupPageViewModelBase
{
    private readonly ILogger _logger;
    private readonly IHost _host;

#pragma warning disable SA1310 // Field names should not contain underscore
    private const int NUMBER_OF_PARALLEL_RUNNING_TASKS = 20;
#pragma warning restore SA1310 // Field names should not contain underscore

#pragma warning disable SA1310 // Field names should not contain underscore
    private const int MAX_RETRIES = 1;
#pragma warning restore SA1310 // Field names should not contain underscore

    private int _retryCount;

    /// <summary>
    /// Event raised when the execution of all tasks is completed.
    /// </summary>
    public event EventHandler ExecutionFinished;

    private readonly SetupFlowOrchestrator orchestrator;

    private IList<TaskInformation> _failedTasks;

    [ObservableProperty]
    private ObservableCollection<TaskInformation> _setupTasks;

    [ObservableProperty]
    private ObservableCollection<ActionCenterMessages> _actionCenterItems;

    [ObservableProperty]
    private int _tasksCompleted;

    [ObservableProperty]
    private int _tasksStarted;

    [ObservableProperty]
    private int _tasksFinishedSuccessfully;

    [ObservableProperty]
    private int _tasksFinishedUnSuccessfully;

    [ObservableProperty]
    private int _tasksThatNeedAttention;

    [ObservableProperty]
    private string _executingTasks;

    [ObservableProperty]
    private string _actionCenterDisplay;

    [ObservableProperty]
    private Visibility _showRetryAndFailedButtons;

    [ObservableProperty]
    private Visibility _showNextButton;

    [ObservableProperty]
    private bool _enableRetryButton;

    [RelayCommand]
    public async void RestartFailedTasks()
    {
        // Keep the number of successful tasks and needs attention tasks the same.
        // Change failed tasks to 0 becuase, once restarted all tasks haven't failed yet.
        TasksStarted = 0;
        TasksFinishedUnSuccessfully = 0;
        SetExecutingTaskAndActionCenter();
        SetupTasks = new ObservableCollection<TaskInformation>(_failedTasks);

        // Empty out the collection since all failed tasks are being re-ran
        _retryCount++;
        ActionCenterItems = new ObservableCollection<ActionCenterMessages>();
        EnableRetryButton = false;
        await StartAllTasks(SetupTasks);
    }

    [RelayCommand]
    public void NextButtonClicked()
    {
        ExecutionFinished.Invoke(null, null);
    }

    public LoadingViewModel(ILogger logger, ISetupFlowStringResource stringResource, IHost host)
        : base(stringResource)
    {
        _logger = logger;
        _host = host;
        _setupTasks = new ();

        IsNavigationBarVisible = false;
        IsStepPage = false;

        orchestrator = _host.GetService<SetupFlowOrchestrator>();
        ShowRetryAndFailedButtons = Visibility.Collapsed;
        _failedTasks = new List<TaskInformation>();
        ActionCenterItems = new ();
        CanGoToNextPage = false;
    }

    /// <summary>
    /// Reads from the orchestrator to get all the tasks to run.
    /// The ordering of the tasks, ordering for dependencies, is done later.
    /// </summary>
    private void FetchTaskInformation()
    {
        var taskIndex = 0;
        foreach (var taskGroup in orchestrator.TaskGroups)
        {
            foreach (var task in taskGroup.SetupTasks)
            {
                SetupTasks.Add(new TaskInformation { TaskIndex = taskIndex++, TaskToExecute = task, MessageToShow = task.GetLoadingMessages().Executing, StatusIconGridVisibility = Visibility.Collapsed });
            }
        }

        SetExecutingTaskAndActionCenter();
    }

    /// <summary>
    /// Simple method to change the ExecutingTasks and ActionCenter messages.
    /// </summary>
    private void SetExecutingTaskAndActionCenter()
    {
        ExecutingTasks = StringResource.GetLocalized(StringResourceKey.LoadingExecutingProgress, TasksStarted, _setupTasks.Count);
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
        var stringToReplace = string.Empty;
        var circleBrush = new SolidColorBrush();
        var statusSymbolHex = string.Empty;

        if (taskFinishedState == TaskFinishedState.Success)
        {
            if (information.TaskToExecute.RequiresReboot)
            {
                stringToReplace = information.TaskToExecute.GetLoadingMessages().NeedsReboot;
                circleBrush.Color = Microsoft.UI.Colors.Yellow;
                statusSymbolHex = "\xF13C";
                ActionCenterItems.Add(information.TaskToExecute.GetRebootMessage());
            }
            else
            {
                stringToReplace = information.TaskToExecute.GetLoadingMessages().Finished;
                circleBrush.Color = Microsoft.UI.Colors.Green;
                statusSymbolHex = "\xE73E";
            }

            TasksFinishedSuccessfully++;
        }
        else if (taskFinishedState == TaskFinishedState.Failure)
        {
            stringToReplace = information.TaskToExecute.GetLoadingMessages().Error;
            circleBrush.Color = Microsoft.UI.Colors.Red;
            statusSymbolHex = "\xF78A";
            ActionCenterItems.Add(information.TaskToExecute.GetErrorMessages());
            TasksFinishedUnSuccessfully++;

            if (_retryCount < MAX_RETRIES)
            {
                _failedTasks.Add(information);
            }
        }

        /*information.StatusIconGridVisibility = Visibility.Visible;*/
        information.CircleForeground = circleBrush;
        information.StatusSymbolHex = statusSymbolHex;
        information.MessageToShow = stringToReplace;
    }

    /// <summary>
    /// gGet all information needed to run all tasks and run them.
    /// </summary>
    public async override void OnNavigateToPageAsync()
    {
        FetchTaskInformation();

        await StartAllTasks(_setupTasks);
    }

    /// <summary>
    /// Starts all the tasks in the passed in ObservableCollection.
    /// </summary>
    /// <param name="tasks">All the tasks to start</param>
    /// <returns>An awaitable task</returns>
    private async Task StartAllTasks(ObservableCollection<TaskInformation> tasks)
    {
        var window = Application.Current.GetService<WindowEx>();
        await Task.Run(async () =>
        {
            var tasksToRunFirst = new List<TaskInformation>();
            var tasksToRunSecond = new List<TaskInformation>();

            // Most likely need a better way to figure out dependncies.
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
        if (_failedTasks.Count == 0 || _retryCount >= MAX_RETRIES)
        {
            // Move to the next screen if either
            // no tasks failed, or
            // user tried re-running them once.
            ExecutionFinished.Invoke(null, null);
        }
        else
        {
            // At this point some tasks ran into an error.
            // Give the user the option to re try them all or move to the next screen.
            ShowRetryAndFailedButtons = Visibility.Visible;

            // tasks were re-tried once.  Don't let users try again.
            EnableRetryButton = _retryCount < MAX_RETRIES;

            // Allow user to retry all failed tasks or go to the next page.
            CanGoToNextPage = true;
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
                ExecutingTasks = StringResource.GetLocalized(StringResourceKey.LoadingExecutingProgress, TasksStarted, _setupTasks.Count);
            });

            var taskFinishedState = await taskInformation.TaskToExecute.Execute();
            window.DispatcherQueue.TryEnqueue(() =>
            {
                ChangeMessage(taskInformation, taskFinishedState);

                TasksCompleted++;
                ActionCenterDisplay = StringResource.GetLocalized(StringResourceKey.ActionCenterDisplay, TasksFinishedUnSuccessfully);
            });
        }
        catch
        {
            // Don't let a single task break everything
            // TODO: Show failed tasks on UI
        }
    }
}
