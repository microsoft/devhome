// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.SetupFlow.DevDrive.Models;
using DevHome.SetupFlow.DevDrive.ViewModels;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.DevDrive;

public class DevDriveTaskGroup : ISetupTaskGroup
{
    private readonly IHost _host;

    public DevDriveTaskGroup(IHost host)
    {
        _host = host;
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
            _devDriveTasks.Add(new CreateDevDriveTask(devDrive));
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

    public IEnumerable<ISetupTask> SetupTasks => _devDriveTasks;

    public SetupPageViewModelBase GetSetupPageViewModel() => null;

    public ReviewTabViewModelBase GetReviewTabViewModel() => _host.CreateInstance<DevDriveReviewViewModel>(this);
}
