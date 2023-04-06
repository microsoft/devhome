// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.TaskGroups;

public class DevDriveTaskGroup : ISetupTaskGroup
{
    private readonly IHost _host;
    private readonly Lazy<DevDriveReviewViewModel> _devDriveReviewViewModel;
    private readonly ISetupFlowStringResource _stringResource;
    private readonly ILogger _logger;

    public DevDriveTaskGroup(IHost host, ILogger logger, ISetupFlowStringResource stringResource)
    {
        _host = host;

        // TODO Remove `this` argument from CreateInstance since this task
        // group is a registered type. This requires updating dependent classes
        // correspondingly.
        _devDriveReviewViewModel = new (() => _host.CreateInstance<DevDriveReviewViewModel>(this));
        _stringResource = stringResource;
        _logger = logger;
    }

    /// <summary>
    /// Update the Dev Drive task with a new Dev Drive object. We currently only
    /// support creating one dev drive at a time.
    /// </summary>
    /// <param name="devDrive">
    /// The dev drive object that will be used to create a dev drive on the system
    /// </param>
    public void AddDevDriveTask(IDevDrive devDrive)
    {
        if (_devDriveTasks.Any())
        {
            _devDriveTasks[0].DevDrive = devDrive;
        }
        else
        {
            _devDriveTasks.Add(new CreateDevDriveTask(devDrive, _host, _logger, _stringResource));
        }
    }

    /// <summary>
    /// Remove all tasks from the task group. Since we only support creating one Dev
    /// Drive at a time we can just clear out the tasks.
    /// </summary>
    public void RemoveDevDriveTasks()
    {
        _devDriveTasks.Clear();
    }

    private readonly IList<CreateDevDriveTask> _devDriveTasks = new List<CreateDevDriveTask>();

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

    public SetupPageViewModelBase GetSetupPageViewModel() => null;

    public ReviewTabViewModelBase GetReviewTabViewModel() => _devDriveReviewViewModel.Value;
}
