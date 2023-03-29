// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.SetupFlow.DevDrive.Models;
using DevHome.SetupFlow.DevDrive.ViewModels;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.DevDrive;

public class DevDriveTaskGroup : ISetupTaskGroup
{
    private readonly IHost _host;

    public bool RequiresReview => true;

    public DevDriveTaskGroup(IHost host)
    {
        _host = host;
    }

    private readonly IList<CreateDevDriveTask> _devDriveTasks = new List<CreateDevDriveTask>();

    public IEnumerable<ISetupTask> SetupTasks => _devDriveTasks;

    public SetupPageViewModelBase GetSetupPageViewModel() => null;

    public ReviewTabViewModelBase GetReviewTabViewModel() => _host.CreateInstance<DevDriveReviewViewModel>(this);
}
