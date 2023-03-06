// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.SetupFlow.DevVolume.Models;
using DevHome.SetupFlow.DevVolume.ViewModels;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.DevVolume;

public class DevVolumeTaskGroup : ISetupTaskGroup
{
    private readonly IHost _host;

    public DevVolumeTaskGroup(IHost host)
    {
        _host = host;
    }

    private readonly IList<CreateDevVolumeTask> _devVolumeTasks = new List<CreateDevVolumeTask>();

    public IEnumerable<ISetupTask> SetupTasks => _devVolumeTasks;

    public SetupPageViewModelBase GetSetupPageViewModel() => _host.CreateInstance<DevVolumeViewModel>(this);

    public ReviewTabViewModelBase GetReviewTabViewModel() => _host.CreateInstance<DevVolumeReviewViewModel>(this);
}
