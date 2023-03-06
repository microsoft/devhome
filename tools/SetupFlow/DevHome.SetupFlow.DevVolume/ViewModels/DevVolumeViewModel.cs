// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.Telemetry;

namespace DevHome.SetupFlow.DevVolume.ViewModels;

public partial class DevVolumeViewModel : SetupPageViewModelBase
{
    private readonly ILogger _logger;
    private readonly DevVolumeTaskGroup _taskGroup;

    public DevVolumeViewModel(ILogger logger, IStringResource stringResource, DevVolumeTaskGroup taskGroup)
        : base(stringResource)
    {
        _logger = logger;
        _taskGroup = taskGroup;
    }
}
