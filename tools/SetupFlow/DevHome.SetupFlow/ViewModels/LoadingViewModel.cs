// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

extern alias Projection;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Renderers;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.Common.Views;
using DevHome.Contracts.Services;
using DevHome.Logging;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;
using Windows.Storage;
using WinUIEx;
using WinUIEx.Messaging;

namespace DevHome.SetupFlow.ViewModels;

public partial class LoadingViewModel : SetupPageViewModelBase
{
    private readonly IHost _host;

    private readonly ElementTheme _currentTheme;

    private readonly Guid _activityId;

    private static readonly BitmapImage DarkCaution = new(new Uri("ms-appx:///DevHome.SetupFlow/Assets/DarkCaution.png"));
    private static readonly BitmapImage DarkError = new(new Uri("ms-appx:///DevHome.SetupFlow/Assets/DarkError.png"));
    private static readonly BitmapImage DarkSuccess = new(new Uri("ms-appx:///DevHome.SetupFlow/Assets/DarkSuccess.png"));
    private static readonly BitmapImage LightCaution = new(new Uri("ms-appx:///DevHome.SetupFlow/Assets/LightCaution.png"));
    private static readonly BitmapImage LightError = new(new Uri("ms-appx:///DevHome.SetupFlow/Assets/LightError.png"));
    private static readonly BitmapImage LightSuccess = new(new Uri("ms-appx:///DevHome.SetupFlow/Assets/LightSuccess.png"));

#pragma warning disable SA1310 // Field names should not contain underscore
    private const int NUMBER_OF_PARALLEL_RUNNING_TASKS = 5;
#pragma warning restore SA1310 // Field names should not contain underscore

#pragma warning disable SA1310 // Field names should not contain underscore
    private const int MAX_RETRIES = 1;
#pragma warning restore SA1310 // Field names should not contain underscore

    private int _retryCount;

    /// <summary>
    /// A business rule for the loading screen is  "executing" messages should appear at the bottom
    /// of the list of messages.  To enable this behavior "finished" messages and "executing-but-done" messages
    /// need to be inserted at the index before the first executing message.
    /// To keep track of the index this is used to count backwards from Messages.count.
    /// </summary>
    private int _numberOfExecutingTasks;

    /// <summary>
    /// Event raised when the execution of all tasks is completed.
    /// </summary>
    public event EventHandler ExecutionFinished;

    /// <summary>
    /// Keep track of all failed tasks so they can be re-ran if the user wishes.
    /// </summary>
    private readonly List<TaskInformation> _failedTasks;

    public IList<TaskInformation> FailedTasks => _failedTasks;

    /// <summary>
    /// All the tasks that will be executed.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<TaskInformation> _tasksToRun;

    [ObservableProperty]
    private ObservableCollection<LoadingMessageViewModel> _messages;

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
    public async Task RestartFailedTasksAsync()
    {
        TelemetryFactory.Get<ITelemetry>().LogCritical("Loading_RestartFailedTasks_Event");
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

    public void AddMessage(string message, MessageSeverityKind severityKind = MessageSeverityKind.Info)
    {
        Application.Current.GetService<WindowEx>().DispatcherQueue.TryEnqueue(() =>
        {
            var messageToDisplay = new LoadingMessageViewModel(message);
            messageToDisplay.ShouldShowStatusSymbolIcon = false;
            messageToDisplay.ShouldShowProgressRing = false;

            if (severityKind == MessageSeverityKind.Warning)
            {
                messageToDisplay.ShouldShowStatusSymbolIcon = true;
                messageToDisplay.StatusSymbolIcon = (_currentTheme == ElementTheme.Dark) ? DarkCaution : LightCaution;
            }
            else if (severityKind == MessageSeverityKind.Error)
            {
                messageToDisplay.ShouldShowStatusSymbolIcon = true;
                messageToDisplay.StatusSymbolIcon = (_currentTheme == ElementTheme.Dark) ? DarkError : LightError;
            }
            else if (severityKind == MessageSeverityKind.Success)
            {
                messageToDisplay.ShouldShowStatusSymbolIcon = true;
                messageToDisplay.StatusSymbolIcon = (_currentTheme == ElementTheme.Dark) ? DarkSuccess : LightSuccess;
            }

            Messages.Insert(Messages.Count - _numberOfExecutingTasks, messageToDisplay);
        });
    }

    public void UpdateActionCenterMessage(ActionCenterMessages message, ActionMessageRequestKind requestKind)
    {
        // ALl referenced to WindowEx and Application.Current will be removed in the future,
        // in the loadingViewModel.
        Application.Current.GetService<WindowEx>().DispatcherQueue.TryEnqueue(() =>
        {
            // We need to add/remove the message in a temporary list and then re add the items to a new collection. This is because
            // of the adaptive card panel and it receiving UI updates in the listview. There can be random crashes if we don't do this when
            // the user switches between different Dev Home pages and then comes back to the loading screen when an adaptive card is
            // loaded into the action center.
            var items = ActionCenterItems.ToList();
            if (requestKind == ActionMessageRequestKind.Add)
            {
                items.Add(message);
            }
            else
            {
                items.Remove(message);
            }

            ActionCenterItems = new(items);
        });
    }

    public LoadingViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        IHost host)
        : base(stringResource, orchestrator)
    {
        _host = host;
        _tasksToRun = new();

        // Assuming that the theme can't change while the user is in the loading screen.
        _currentTheme = _host.GetService<IThemeSelectorService>().Theme;

        IsStepPage = false;
        IsNavigationBarVisible = false;
        NextPageButtonText = stringResource.GetLocalized(StringResourceKey.LoadingScreenGoToSummaryButtonContent);
        ShowRetryButton = Visibility.Collapsed;
        _failedTasks = new List<TaskInformation>();
        ActionCenterItems = new();
        Messages = new();
        _activityId = orchestrator.ActivityId;
    }

    // Remove all tasks except for the SetupTarget

    /// <summary>
    /// Reads from the orchestrator to get all the tasks to run.
    /// The ordering of the tasks, ordering for dependencies, is done later.
    /// </summary>
    private void FetchTaskInformation()
    {
        Log.Logger?.ReportDebug(Log.Component.Loading, "Fetching task information");
        var taskIndex = 0;

        if (Orchestrator.IsSettingUpATargetMachine)
        {
            var taskGroup = Orchestrator.GetTaskGroup<SetupTargetTaskGroup>();
            var task = taskGroup.SetupTasks.First();
            task.AddMessage += AddMessage;
            task.UpdateActionCenterMessage += UpdateActionCenterMessage;
            TasksToRun.Add(new TaskInformation
            {
                TaskIndex = taskIndex++,
                TaskToExecute = task,
                MessageToShow = task.GetLoadingMessages().Executing,
            });
            SetExecutingTaskAndActionCenter();
            return;
        }

        foreach (var taskGroup in Orchestrator.TaskGroups)
        {
            foreach (var task in taskGroup.SetupTasks)
            {
                task.AddMessage += AddMessage;
                TasksToRun.Add(new TaskInformation
                {
                    TaskIndex = taskIndex++,
                    TaskToExecute = task,
                    MessageToShow = task.GetLoadingMessages().Executing,
                });
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
    private void ChangeMessage(TaskInformation information, LoadingMessageViewModel loadingMessage, TaskFinishedState taskFinishedState)
    {
        Log.Logger?.ReportDebug(Log.Component.Loading, $"Updating message for task {information.MessageToShow} with state {taskFinishedState}");
        var stringToReplace = string.Empty;
        BitmapImage statusSymbolIcon = null;

        // Two things to do.
        // 1. Change the message color and icon in information
        // 2. Add a new message with the done message.
        if (taskFinishedState == TaskFinishedState.Success)
        {
            if (information.TaskToExecute.RequiresReboot)
            {
                Log.Logger?.ReportDebug(Log.Component.Loading, "Task succeeded but requires reboot; adding to action center");
                stringToReplace = information.TaskToExecute.GetLoadingMessages().NeedsReboot;
                statusSymbolIcon = (_currentTheme == ElementTheme.Dark) ? DarkCaution : LightCaution;
                ActionCenterItems.Insert(0, information.TaskToExecute.GetRebootMessage());
            }
            else
            {
                Log.Logger?.ReportDebug(Log.Component.Loading, "Task succeeded");
                stringToReplace = information.TaskToExecute.GetLoadingMessages().Finished;
                statusSymbolIcon = (_currentTheme == ElementTheme.Dark) ? DarkSuccess : LightSuccess;
            }

            TasksFinishedSuccessfully++;
        }
        else if (taskFinishedState == TaskFinishedState.Failure)
        {
            Log.Logger?.ReportDebug(Log.Component.Loading, "Task failed");
            stringToReplace = information.TaskToExecute.GetLoadingMessages().Error;
            statusSymbolIcon = (_currentTheme == ElementTheme.Dark) ? DarkError : LightError;
            ActionCenterItems.Insert(0, information.TaskToExecute.GetErrorMessages());
            TasksFailed++;

            Log.Logger?.ReportDebug(Log.Component.Loading, "Adding task to list for retry");
            _failedTasks.Add(information);
        }

        // When a task is done
        // Following logic is to keep all "executing" messages at the bottom of the list.
        // Remove the "executing" message from the list.
        Messages.Remove(loadingMessage);

        // Modify the message so it looks done.
        loadingMessage.ShouldShowProgressRing = false;

        // Insert the message right before any "executing" messages.
        Messages.Insert(Messages.Count - _numberOfExecutingTasks, loadingMessage);

        // Add the "Execution finished" message
        var newLoadingScreenMessage = new LoadingMessageViewModel(stringToReplace);
        newLoadingScreenMessage.StatusSymbolIcon = statusSymbolIcon;
        newLoadingScreenMessage.ShouldShowProgressRing = false;
        newLoadingScreenMessage.ShouldShowStatusSymbolIcon = true;

        // Insert the message right before any "executing" messages.
        Messages.Insert(Messages.Count - _numberOfExecutingTasks, newLoadingScreenMessage);
    }

    /// <summary>
    /// Get all information needed to run all tasks and run them.
    /// </summary>
    protected async override Task OnFirstNavigateToAsync()
    {
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
        if (_failedTasks.Count == 0)
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

        if (_failedTasks.Count != 0)
        {
            TelemetryFactory.Get<ITelemetry>().Log("Loading_FailedTasks_Event", LogLevel.Critical, new LoadingRetryEvent(_failedTasks.Count), _activityId);
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
            var loadingMessage = new LoadingMessageViewModel(taskInformation.MessageToShow);
            window.DispatcherQueue.TryEnqueue(() =>
            {
                TasksStarted++;
                ExecutingTasks = StringResource.GetLocalized(StringResourceKey.LoadingExecutingProgress, TasksStarted, TasksToRun.Count);

                loadingMessage.ShouldShowProgressRing = true;
                Messages.Add(loadingMessage);

                // Keep increment inside TryEnqueue to enforce "locking"
                _numberOfExecutingTasks++;
            });

            TaskFinishedState taskFinishedState;
            if (taskInformation.TaskToExecute.RequiresAdmin && Orchestrator.RemoteElevatedOperation != null)
            {
                Log.Logger?.ReportInfo(Log.Component.Loading, "Starting task as admin");
                taskFinishedState = await taskInformation.TaskToExecute.ExecuteAsAdmin(Orchestrator.RemoteElevatedOperation.Value);
            }
            else
            {
                taskFinishedState = await taskInformation.TaskToExecute.Execute();
            }

            window.DispatcherQueue.TryEnqueue(() =>
            {
                // Keep decrement inside TryEnqueue to encorce "locking"
                _numberOfExecutingTasks--;
                ChangeMessage(taskInformation, loadingMessage, taskFinishedState);
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
