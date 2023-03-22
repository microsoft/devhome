// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
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

    /// <summary>
    /// Event raised when the execution of all tasks is completed.
    /// </summary>
    public event EventHandler ExecutionFinished;

    private readonly SetupFlowOrchestrator orchestrator;

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

    public LoadingViewModel(ILogger logger, ISetupFlowStringResource stringResource, IHost host)
        : base(stringResource)
    {
        _logger = logger;
        _host = host;
        _setupTasks = new ();

        IsNavigationBarVisible = false;
        IsStepPage = false;

        orchestrator = _host.GetService<SetupFlowOrchestrator>();
        ActionCenterItems = new ();
    }

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

        ExecutingTasks = StringResource.GetLocalized(StringResourceKey.LoadingExecutingProgress, TasksStarted, _setupTasks.Count);
        ActionCenterDisplay = StringResource.GetLocalized(StringResourceKey.ActionCenterDisplay, 0, 0, 0);
    }

    public void ChangeMessage(int taskNumber, TaskFinishedState taskFinishedState)
    {
        var information = SetupTasks[taskNumber];
        var stringToReplace = string.Empty;
        var circleBrush = new SolidColorBrush();
        var statusSymbolHex = string.Empty;

        if (taskFinishedState == TaskFinishedState.Success)
        {
            stringToReplace = information.TaskToExecute.GetLoadingMessages().Finished;
            circleBrush.Color = Microsoft.UI.Colors.Green;
            statusSymbolHex = "\xF13E";
        }
        else if (taskFinishedState == TaskFinishedState.Failure)
        {
            stringToReplace = information.TaskToExecute.GetLoadingMessages().Error;
            ActionCenterItems.Add(information.TaskToExecute.GetErrorMessages());
            circleBrush.Color = Microsoft.UI.Colors.Red;
            statusSymbolHex = "\xE711";
        }
        else if (taskFinishedState == TaskFinishedState.NeedsAttention)
        {
            stringToReplace = information.TaskToExecute.GetLoadingMessages().NeedsAttention;
            ActionCenterItems.Add(information.TaskToExecute.GetErrorMessages());
            circleBrush.Color = Microsoft.UI.Colors.Green;
            statusSymbolHex = "\xF13E";
        }

        information.MessageToShow = stringToReplace;
        information.StatusIconGridVisibility = Visibility.Visible;
        information.CircleForeground = circleBrush;
        information.StatusSymbolHex = statusSymbolHex;

        // change a value in the list to force UI to update
        SetupTasks[taskNumber] = information;
    }

    public async override void OnNavigateToPageAsync()
    {
        FetchTaskInformation();

        var window = Application.Current.GetService<WindowEx>();
        await Task.Run(() =>
        {
            var tasksToRunFirst = new List<TaskInformation>();
            var tasksToRunSecond = new List<TaskInformation>();
            foreach (var taskInformation in _setupTasks)
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

            var blah = Parallel.ForEach(tasksToRunFirst, async task =>
            {
                await StartTaskAndReportResult(window, task);
            });

            while (!blah.IsCompleted)
            {
                Thread.Sleep(1000);
            }

            Parallel.ForEach(tasksToRunSecond, async task =>
            {
                await StartTaskAndReportResult(window, task);
            });
        });

        ExecutionFinished.Invoke(null, null);
    }

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
            taskFinishedState = TaskFinishedState.Failure;
            window.DispatcherQueue.TryEnqueue(() =>
            {
                ChangeMessage(taskInformation.TaskIndex, taskFinishedState);

                TasksCompleted++;
                TasksFinishedSuccessfully++;
                ActionCenterDisplay = StringResource.GetLocalized(StringResourceKey.ActionCenterDisplay, TasksFinishedSuccessfully, TasksFinishedUnSuccessfully, TasksThatNeedAttention);
            });
        }
        catch
        {
            // Don't let a single task break everything
            // TODO: Show failed tasks on UI
        }
    }
}
