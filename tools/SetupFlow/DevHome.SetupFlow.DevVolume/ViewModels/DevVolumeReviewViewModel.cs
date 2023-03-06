// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.Telemetry;

namespace DevHome.SetupFlow.DevVolume.ViewModels;

public partial class DevVolumeReviewViewModel : ReviewTabViewModelBase
{
    private readonly ILogger _logger;
    private readonly IStringResource _stringResource;
    private readonly DevVolumeTaskGroup _taskGroup;

    public DevVolumeReviewViewModel(ILogger logger, IStringResource stringResource, DevVolumeTaskGroup taskGroup)
    {
        _logger = logger;
        _stringResource = stringResource;
        _taskGroup = taskGroup;

        TabTitle = stringResource.GetLocalized(StringResourceKey.Basics);
    }
}
