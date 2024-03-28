// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DevHome.SetupFlow.TaskGroups;

public class DevDriveTaskGroup : ISetupTaskGroup
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(DevDriveTaskGroup));
    private readonly IHost _host;
    private readonly Lazy<DevDriveReviewViewModel> _devDriveReviewViewModel;
    private readonly ISetupFlowStringResource _stringResource;

    public DevDriveTaskGroup(IHost host, ISetupFlowStringResource stringResource)
    {
        _host = host;

        // TODO https://github.com/microsoft/devhome/issues/631
        _devDriveReviewViewModel = new(() => new DevDriveReviewViewModel(host, stringResource, this));
        _stringResource = stringResource;
    }

    /// <summary>
    /// Update the Dev Drive task with a new Dev Drive object. Currently only
    /// one dev drive can be created at a time.
    /// </summary>
    /// <param name="devDrive">
    /// The dev drive object that will be used to create a dev drive on the system
    /// </param>
    public void AddDevDriveTask(IDevDrive devDrive)
    {
        if (_devDriveTasks.Count != 0)
        {
            _log.Information($"Overwriting existing dev drive task");
            _devDriveTasks[0].DevDrive = devDrive;
        }
        else
        {
            _log.Information("Adding new dev drive task");
            _devDriveTasks.Add(new CreateDevDriveTask(devDrive, _host, _host.GetService<SetupFlowOrchestrator>().ActivityId, _stringResource));
        }
    }

    /// <summary>
    /// Remove all tasks from the task group. This is okay because only one Dev
    /// Drive can be created at a time.
    /// </summary>
    public void RemoveDevDriveTasks()
    {
        _log.Information("Clearing all dev drive tasks");
        _devDriveTasks.Clear();
    }

    private readonly List<CreateDevDriveTask> _devDriveTasks =
        [
        ];

    public IEnumerable<ISetupTask> SetupTasks
    {
        get
        {
            if (_host.GetService<IDevDriveManager>().RepositoriesUsingDevDrive <= 0)
            {
                return new List<ISetupTask>();
            }

            return _devDriveTasks;
        }
    }

    public IEnumerable<ISetupTask> DSCTasks => SetupTasks;

    public SetupPageViewModelBase GetSetupPageViewModel() => null;

    // Only show this tab when actually creating a dev drive
    public ReviewTabViewModelBase GetReviewTabViewModel() => _devDriveReviewViewModel.Value;
}
