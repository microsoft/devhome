// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
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
    private int _tasksCompleted;

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
    }

    private void FetchTaskInformation()
    {
        var taskIndex = 0;
        foreach (var taskGroup in orchestrator.TaskGroups)
        {
            foreach (var task in taskGroup.SetupTasks)
            {
                _setupTasks.Add(new TaskInformation { TaskIndex = taskIndex++, TaskToExecute = task, MessageToShow = task.GetLoadingMessages().Executing });
            }
        }

        /*
         * These strings need to be localized.
         */
        ExecutingTasks = $"Executing Step {TasksCompleted}/{_setupTasks.Count}";
        ActionCenterDisplay = "Succeeded: 0 - Failed: 0 - Needs Attention: 0";
    }

    public void ChangeMessage(int taskNumber, TaskFinishedState taskFinishedState)
    {
        var information = _setupTasks[taskNumber];
        var stringToReplace = string.Empty;

        if (taskFinishedState == TaskFinishedState.Success)
        {
            stringToReplace = information.TaskToExecute.GetLoadingMessages().Finished;
        }
        else if (taskFinishedState == TaskFinishedState.Failure)
        {
            stringToReplace = information.TaskToExecute.GetLoadingMessages().Error;
        }
        else if (taskFinishedState == TaskFinishedState.NeedsAttention)
        {
            stringToReplace = information.TaskToExecute.GetLoadingMessages().NeedsAttention;
        }

        // change a value in the list to force UI to update
        _setupTasks[taskNumber].MessageToShow = stringToReplace;
    }

    public async override void OnNavigateToPageAsync()
    {
        FetchTaskInformation();

        var window = Application.Current.GetService<WindowEx>();
        await Task.Run(() =>
        {
            Parallel.ForEach(_setupTasks, async task =>
            {
                // Start the task and wait for it to complete.
                try
                {
                    var taskFinishedState = await task.TaskToExecute.Execute();
                    window.DispatcherQueue.TryEnqueue(() =>
                    {
                        ChangeMessage(task.TaskIndex, taskFinishedState);
                        TasksFinishedSuccessfully++;
                        TasksCompleted++;
                        ExecutingTasks = $"Executing Step {TasksCompleted}/{_setupTasks.Count}";
                        ActionCenterDisplay = $"Succeeded: {TasksFinishedSuccessfully} - Failed: {TasksFinishedUnSuccessfully} - Needs Attention: {TasksThatNeedAttention}";
                    });
                }
                catch
                {
                    // Don't let a single task break everything
                    // TODO: Show failed tasks on UI
                }
            });
        });

        ExecutionFinished.Invoke(null, null);
    }
}
